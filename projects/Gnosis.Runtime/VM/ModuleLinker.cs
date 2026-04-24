namespace Gnosis.Runtime.VM;

/// <summary>
/// 模块链接结果
/// </summary>
public sealed class ModuleLinkResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<ResolvedImport> ResolvedImports { get; }
    public IReadOnlyList<UnresolvedImport> UnresolvedImports { get; }

    public ModuleLinkResult(bool success, string? errorMessage,
        IReadOnlyList<ResolvedImport> resolvedImports,
        IReadOnlyList<UnresolvedImport> unresolvedImports)
    {
        Success = success;
        ErrorMessage = errorMessage;
        ResolvedImports = resolvedImports;
        UnresolvedImports = unresolvedImports;
    }

    public static ModuleLinkResult Ok(IReadOnlyList<ResolvedImport> resolved) =>
        new(true, null, resolved, []);

    public static ModuleLinkResult Fail(string message, IReadOnlyList<UnresolvedImport> unresolved) =>
        new(false, message, [], unresolved);
}

/// <summary>
/// 已解析的导入符号
/// </summary>
public sealed record ResolvedImport(
    string ImportModuleName,
    string SymbolName,
    string ExportModuleName,
    int FunctionEntryOffset,
    int ParameterCount);

/// <summary>
/// 未解析的导入符号
/// </summary>
public sealed record UnresolvedImport(
    string ImportModuleName,
    string SymbolName,
    string Reason);

/// <summary>
/// 模块链接器，解析模块间的导入/导出符号依赖
/// </summary>
public sealed class ModuleLinker
{
    #region 公共方法

    /// <summary>
    /// 链接一组模块，解析所有导入/导出依赖
    /// </summary>
    public ModuleLinkResult Link(IReadOnlyList<IModule> modules)
    {
        if (modules.Count == 0)
        {
            return ModuleLinkResult.Ok([]);
        }

        var cycleError = DetectCycles(modules);
        if (cycleError is not null)
        {
            return ModuleLinkResult.Fail($"检测到循环依赖：{cycleError}", []);
        }

        var exportIndex = BuildExportIndex(modules);
        var resolved = new List<ResolvedImport>();
        var unresolved = new List<UnresolvedImport>();

        foreach (var module in modules)
        {
            foreach (var importSymbol in module.ImportedSymbols)
            {
                var resolvedImport = ResolveImport(module.Name, importSymbol, exportIndex);
                if (resolvedImport is not null)
                {
                    resolved.Add(resolvedImport);
                }
                else
                {
                    unresolved.Add(new UnresolvedImport(module.Name, importSymbol, "未找到匹配的导出符号"));
                }
            }
        }

        if (unresolved.Count > 0)
        {
            return ModuleLinkResult.Fail(
                $"存在 {unresolved.Count} 个未解析的导入符号",
                unresolved);
        }

        return ModuleLinkResult.Ok(resolved);
    }

    /// <summary>
    /// 验证模块是否可以安全加载（所有导入依赖都已满足）
    /// </summary>
    public bool CanLoad(IModule module, IReadOnlyList<IModule> existingModules)
    {
        var allModules = new List<IModule>(existingModules) { module };
        var exportIndex = BuildExportIndex(allModules);

        foreach (var importSymbol in module.ImportedSymbols)
        {
            if (!IsSymbolInExportIndex(importSymbol, exportIndex))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取模块的依赖模块名称列表
    /// </summary>
    public IReadOnlyList<string> GetDependencies(IModule module)
    {
        var dependencies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var importSymbol in module.ImportedSymbols)
        {
            var separatorIndex = importSymbol.IndexOf("::", StringComparison.Ordinal);
            if (separatorIndex >= 0)
            {
                dependencies.Add(importSymbol[..separatorIndex]);
            }
        }

        return dependencies.ToList();
    }

    /// <summary>
    /// 按拓扑排序返回模块加载顺序
    /// </summary>
    public IReadOnlyList<IModule> TopologicalSort(IReadOnlyList<IModule> modules)
    {
        var moduleMap = new Dictionary<string, IModule>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            moduleMap[module.Name] = module;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<IModule>();

        foreach (var module in modules)
        {
            VisitModule(module.Name, moduleMap, visited, visiting, result);
        }

        return result;
    }

    #endregion

    #region 私有方法

    private static Dictionary<string, List<(string ModuleName, ModuleFunctionInfo Function)>> BuildExportIndex(
        IReadOnlyList<IModule> modules)
    {
        var index = new Dictionary<string, List<(string, ModuleFunctionInfo)>>(StringComparer.Ordinal);

        foreach (var module in modules)
        {
            foreach (var exportSymbol in module.ExportedSymbols)
            {
                if (!index.TryGetValue(exportSymbol, out var list))
                {
                    list = new List<(string, ModuleFunctionInfo)>();
                    index[exportSymbol] = list;
                }

                foreach (var func in module.Functions)
                {
                    if (func.Name == exportSymbol || $"{module.Name}::{func.Name}" == exportSymbol)
                    {
                        list.Add((module.Name, func));
                    }
                }
            }
        }

        return index;
    }

    private static bool IsSymbolInExportIndex(string symbol,
        Dictionary<string, List<(string ModuleName, ModuleFunctionInfo Function)>> exportIndex)
    {
        return exportIndex.ContainsKey(symbol);
    }

    private static ResolvedImport? ResolveImport(string importModuleName, string importSymbol,
        Dictionary<string, List<(string ModuleName, ModuleFunctionInfo Function)>> exportIndex)
    {
        if (!exportIndex.TryGetValue(importSymbol, out var exports) || exports.Count == 0)
        {
            return null;
        }

        var export = exports[0];
        return new ResolvedImport(
            importModuleName,
            importSymbol,
            export.ModuleName,
            export.Function.EntryOffset,
            export.Function.ParameterCount);
    }

    private static string? DetectCycles(IReadOnlyList<IModule> modules)
    {
        var moduleMap = new Dictionary<string, IModule>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            moduleMap[module.Name] = module;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var module in modules)
        {
            var cycle = DetectCycleDFS(module.Name, moduleMap, visited, visiting, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;
    }

    private static string? DetectCycleDFS(
        string moduleName,
        Dictionary<string, IModule> moduleMap,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<string> path)
    {
        if (visiting.Contains(moduleName))
        {
            var cycleStart = path.IndexOf(moduleName);
            if (cycleStart >= 0)
            {
                var cyclePath = path.Skip(cycleStart).Append(moduleName);
                return string.Join(" → ", cyclePath);
            }

            return moduleName;
        }

        if (visited.Contains(moduleName))
        {
            return null;
        }

        if (!moduleMap.TryGetValue(moduleName, out var module))
        {
            return null;
        }

        visiting.Add(moduleName);
        path.Add(moduleName);

        foreach (var importSymbol in module.ImportedSymbols)
        {
            var separatorIndex = importSymbol.IndexOf("::", StringComparison.Ordinal);
            var depModuleName = separatorIndex >= 0
                ? importSymbol[..separatorIndex]
                : importSymbol;

            var cycle = DetectCycleDFS(depModuleName, moduleMap, visited, visiting, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        visiting.Remove(moduleName);
        path.RemoveAt(path.Count - 1);
        visited.Add(moduleName);

        return null;
    }

    private static void VisitModule(
        string moduleName,
        Dictionary<string, IModule> moduleMap,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<IModule> result)
    {
        if (visited.Contains(moduleName) || !moduleMap.TryGetValue(moduleName, out var module))
        {
            return;
        }

        if (visiting.Contains(moduleName))
        {
            return;
        }

        visiting.Add(moduleName);

        foreach (var importSymbol in module.ImportedSymbols)
        {
            var separatorIndex = importSymbol.IndexOf("::", StringComparison.Ordinal);
            var depModuleName = separatorIndex >= 0
                ? importSymbol[..separatorIndex]
                : importSymbol;

            VisitModule(depModuleName, moduleMap, visited, visiting, result);
        }

        visiting.Remove(moduleName);
        visited.Add(moduleName);
        result.Add(module);
    }

    #endregion
}
