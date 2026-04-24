using Gnosis.IR.Instruction;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Verse;

/// <summary>
/// Verse 模块加载器，将 Verse 编译产出的 BytecodeUnit 加载到 VM 中
/// </summary>
public sealed class VerseModuleLoader
{
    #region 字段

    private readonly IVMState _vmState;
    private readonly VerseNativeBridge _bridge;
    private readonly VerseStoryRuntime _runtime;
    private readonly ModuleLinker _linker;
    private readonly Dictionary<string, BytecodeModuleAdapter> _loadedModules = new(StringComparer.Ordinal);

    #endregion

    #region 构造函数

    public VerseModuleLoader(IVMState vmState, VerseNativeBridge bridge, VerseStoryRuntime runtime)
    {
        _vmState = vmState;
        _bridge = bridge;
        _runtime = runtime;
        _linker = new ModuleLinker();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 从 BytecodeUnit 加载 Verse 模块到 VM
    /// </summary>
    public bool LoadModule(BytecodeUnit unit)
    {
        var adapter = new BytecodeModuleAdapter(unit);

        if (!adapter.IsValid)
        {
            return false;
        }

        var existingModules = _vmState.Modules;
        if (!_linker.CanLoad(adapter, existingModules))
        {
            return false;
        }

        RegisterSceneFunctions(unit);

        _vmState.LoadModule(adapter);
        _loadedModules[unit.ModuleName] = adapter;
        return true;
    }

    /// <summary>
    /// 批量加载多个模块，自动处理依赖顺序
    /// </summary>
    public int LoadModules(IReadOnlyList<BytecodeUnit> units)
    {
        var adapters = new List<BytecodeModuleAdapter>();
        foreach (var unit in units)
        {
            var adapter = new BytecodeModuleAdapter(unit);
            if (adapter.IsValid)
            {
                adapters.Add(adapter);
            }
        }

        var linkResult = _linker.Link(adapters);
        if (!linkResult.Success)
        {
            return 0;
        }

        var sorted = _linker.TopologicalSort(adapters);
        var loadedCount = 0;

        foreach (var module in sorted)
        {
            if (_loadedModules.ContainsKey(module.Name))
            {
                continue;
            }

            RegisterSceneFunctionsForModule(module);
            _vmState.LoadModule(module);
            _loadedModules[module.Name] = (BytecodeModuleAdapter)module;
            loadedCount++;
        }

        return loadedCount;
    }

    /// <summary>
    /// 卸载 Verse 模块
    /// </summary>
    public void UnloadModule(string moduleName)
    {
        if (_loadedModules.TryGetValue(moduleName, out var adapter))
        {
            _vmState.UnloadModule(moduleName);
            _loadedModules.Remove(moduleName);
        }
    }

    /// <summary>
    /// 获取已加载的模块
    /// </summary>
    public BytecodeModuleAdapter? GetModule(string moduleName)
    {
        return _loadedModules.TryGetValue(moduleName, out var adapter) ? adapter : null;
    }

    /// <summary>
    /// 获取所有已加载模块的名称
    /// </summary>
    public IReadOnlyList<string> GetLoadedModuleNames()
    {
        return _loadedModules.Keys.ToList();
    }

    /// <summary>
    /// 验证所有已加载模块的链接完整性
    /// </summary>
    public ModuleLinkResult ValidateLinks()
    {
        var modules = new List<IModule>(_vmState.Modules);
        return _linker.Link(modules);
    }

    #endregion

    #region 私有方法

    private void RegisterSceneFunctions(BytecodeUnit unit)
    {
        foreach (var function in unit.Functions)
        {
            if (function.Name.StartsWith("verse_scene_"))
            {
                var sceneName = function.Name["verse_scene_".Length..];
                _runtime.RegisterScene(sceneName, new VerseSceneInfo(sceneName, []));
            }
        }
    }

    private void RegisterSceneFunctionsForModule(IModule module)
    {
        foreach (var func in module.Functions)
        {
            if (func.Name.StartsWith("verse_scene_"))
            {
                var sceneName = func.Name["verse_scene_".Length..];
                _runtime.RegisterScene(sceneName, new VerseSceneInfo(sceneName, []));
            }
        }
    }

    #endregion
}
