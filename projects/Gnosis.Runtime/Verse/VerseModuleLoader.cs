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
    private readonly Dictionary<string, BytecodeModuleAdapter> _loadedModules = new(StringComparer.Ordinal);

    #endregion

    #region 构造函数

    public VerseModuleLoader(IVMState vmState, VerseNativeBridge bridge, VerseStoryRuntime runtime)
    {
        _vmState = vmState;
        _bridge = bridge;
        _runtime = runtime;
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

        RegisterSceneFunctions(unit);

        _vmState.LoadModule(adapter);
        _loadedModules[unit.ModuleName] = adapter;
        return true;
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

    #endregion
}
