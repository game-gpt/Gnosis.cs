using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.HotReload;

/// <summary>
/// 模块元数据比对结果
/// </summary>
public sealed class ModuleDiff
{
    #region Properties

    /// <summary>
    /// 是否兼容（可以安全热重载）
    /// </summary>
    public bool IsCompatible { get; init; }

    /// <summary>
    /// 不兼容原因
    /// </summary>
    public string? IncompatibilityReason { get; init; }

    /// <summary>
    /// 新增的函数
    /// </summary>
    public IReadOnlyList<ModuleFunctionInfo> AddedFunctions { get; init; } = [];

    /// <summary>
    /// 移除的函数
    /// </summary>
    public IReadOnlyList<ModuleFunctionInfo> RemovedFunctions { get; init; } = [];

    /// <summary>
    /// 修改的函数（签名变化）
    /// </summary>
    public IReadOnlyList<FunctionChange> ChangedFunctions { get; init; } = [];

    /// <summary>
    /// 新增的类型
    /// </summary>
    public IReadOnlyList<ModuleTypeInfo> AddedTypes { get; init; } = [];

    /// <summary>
    /// 移除的类型
    /// </summary>
    public IReadOnlyList<ModuleTypeInfo> RemovedTypes { get; init; } = [];

    /// <summary>
    /// 新增的全局变量索引
    /// </summary>
    public IReadOnlyList<int> AddedGlobals { get; init; } = [];

    /// <summary>
    /// 移除的全局变量索引
    /// </summary>
    public IReadOnlyList<int> RemovedGlobals { get; init; } = [];

    /// <summary>
    /// 函数地址映射（旧函数 → 新函数）
    /// </summary>
    public IReadOnlyDictionary<CallFrameInfo, CallFrameInfo> FunctionMappings { get; init; }
        = new Dictionary<CallFrameInfo, CallFrameInfo>();

    #endregion
}

/// <summary>
/// 函数变更信息
/// </summary>
public sealed class FunctionChange
{
    #region Properties

    public ModuleFunctionInfo OldFunction { get; }
    public ModuleFunctionInfo NewFunction { get; }
    public bool ParameterCountChanged { get; }
    public bool LocalCountChanged { get; }

    #endregion

    #region Constructors

    public FunctionChange(ModuleFunctionInfo oldFunction, ModuleFunctionInfo newFunction)
    {
        OldFunction = oldFunction;
        NewFunction = newFunction;
        ParameterCountChanged = oldFunction.ParameterCount != newFunction.ParameterCount;
        LocalCountChanged = oldFunction.LocalCount != newFunction.LocalCount;
    }

    #endregion
}

/// <summary>
/// 模块元数据比对器
/// </summary>
public sealed class MetadataComparer
{
    #region 公开方法

    /// <summary>
    /// 比较两个模块的元数据差异
    /// </summary>
    public ModuleDiff Compare(IModule oldModule, IModule newModule)
    {
        var addedFunctions = new List<ModuleFunctionInfo>();
        var removedFunctions = new List<ModuleFunctionInfo>();
        var changedFunctions = new List<FunctionChange>();

        CompareFunctions(oldModule, newModule, addedFunctions, removedFunctions, changedFunctions);

        var addedTypes = new List<ModuleTypeInfo>();
        var removedTypes = new List<ModuleTypeInfo>();

        CompareTypes(oldModule, newModule, addedTypes, removedTypes);

        var addedGlobals = new List<int>();
        var removedGlobals = new List<int>();

        CompareGlobals(oldModule, newModule, addedGlobals, removedGlobals);

        var isCompatible = DetermineCompatibility(removedFunctions, changedFunctions, removedTypes);
        var incompatibilityReason = isCompatible ? null : BuildIncompatibilityReason(removedFunctions, changedFunctions, removedTypes);

        return new ModuleDiff
        {
            IsCompatible = isCompatible,
            IncompatibilityReason = incompatibilityReason,
            AddedFunctions = addedFunctions,
            RemovedFunctions = removedFunctions,
            ChangedFunctions = changedFunctions,
            AddedTypes = addedTypes,
            RemovedTypes = removedTypes,
            AddedGlobals = addedGlobals,
            RemovedGlobals = removedGlobals
        };
    }

    #endregion

    #region 函数比对

    private static void CompareFunctions(
        IModule oldModule, IModule newModule,
        List<ModuleFunctionInfo> added,
        List<ModuleFunctionInfo> removed,
        List<FunctionChange> changed)
    {
        var oldFuncs = oldModule.Functions.ToDictionary(f => f.Name);
        var newFuncs = newModule.Functions.ToDictionary(f => f.Name);

        foreach (var kvp in oldFuncs)
        {
            if (!newFuncs.ContainsKey(kvp.Key))
            {
                removed.Add(kvp.Value);
            }
        }

        foreach (var kvp in newFuncs)
        {
            if (!oldFuncs.ContainsKey(kvp.Key))
            {
                added.Add(kvp.Value);
            }
            else
            {
                var oldFunc = oldFuncs[kvp.Key];
                var newFunc = kvp.Value;

                if (oldFunc.ParameterCount != newFunc.ParameterCount || oldFunc.LocalCount != newFunc.LocalCount)
                {
                    changed.Add(new FunctionChange(oldFunc, newFunc));
                }
            }
        }
    }

    #endregion

    #region 类型比对

    private static void CompareTypes(
        IModule oldModule, IModule newModule,
        List<ModuleTypeInfo> added,
        List<ModuleTypeInfo> removed)
    {
        var oldTypes = oldModule.Types.ToDictionary(t => t.Name);
        var newTypes = newModule.Types.ToDictionary(t => t.Name);

        foreach (var kvp in oldTypes)
        {
            if (!newTypes.ContainsKey(kvp.Key))
            {
                removed.Add(kvp.Value);
            }
        }

        foreach (var kvp in newTypes)
        {
            if (!oldTypes.ContainsKey(kvp.Key))
            {
                added.Add(kvp.Value);
            }
        }
    }

    #endregion

    #region 全局变量比对

    private static void CompareGlobals(
        IModule oldModule, IModule newModule,
        List<int> added,
        List<int> removed)
    {
        var oldConstants = oldModule.Constants.Keys
            .Where(k => int.TryParse(k, out _))
            .Select(int.Parse)
            .ToHashSet();
        var newConstants = newModule.Constants.Keys
            .Where(k => int.TryParse(k, out _))
            .Select(int.Parse)
            .ToHashSet();

        foreach (var idx in newConstants)
        {
            if (!oldConstants.Contains(idx))
            {
                added.Add(idx);
            }
        }

        foreach (var idx in oldConstants)
        {
            if (!newConstants.Contains(idx))
            {
                removed.Add(idx);
            }
        }
    }

    #endregion

    #region 兼容性判断

    private static bool DetermineCompatibility(
        List<ModuleFunctionInfo> removedFunctions,
        List<FunctionChange> changedFunctions,
        List<ModuleTypeInfo> removedTypes)
    {
        if (removedFunctions.Count > 0)
        {
            return false;
        }

        if (removedTypes.Count > 0)
        {
            return false;
        }

        foreach (var change in changedFunctions)
        {
            if (change.ParameterCountChanged)
            {
                return false;
            }
        }

        return true;
    }

    private static string? BuildIncompatibilityReason(
        List<ModuleFunctionInfo> removedFunctions,
        List<FunctionChange> changedFunctions,
        List<ModuleTypeInfo> removedTypes)
    {
        var reasons = new List<string>();

        if (removedFunctions.Count > 0)
        {
            reasons.Add($"移除了 {removedFunctions.Count} 个函数");
        }

        if (removedTypes.Count > 0)
        {
            reasons.Add($"移除了 {removedTypes.Count} 个类型");
        }

        var paramChanges = changedFunctions.Count(c => c.ParameterCountChanged);

        if (paramChanges > 0)
        {
            reasons.Add($"{paramChanges} 个函数参数数量变化");
        }

        return reasons.Count > 0 ? string.Join("，", reasons) : null;
    }

    #endregion
}
