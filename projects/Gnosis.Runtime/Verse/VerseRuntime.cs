using Gnosis.IR.Instruction;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Verse;

/// <summary>
/// Verse 运行时，组合 VM 解释器、原生命令桥接、模块加载器和剧本运行时
/// </summary>
public sealed class VerseRuntime : IDisposable
{
    #region 字段

    private readonly VMInterpreter _interpreter;
    private readonly NativeFunctionRegistry _nativeRegistry;
    private readonly VerseStoryRuntime _storyRuntime;
    private readonly VerseNativeBridge _bridge;
    private readonly VerseModuleLoader _moduleLoader;
    private readonly VMState _vmState;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 剧本运行时
    /// </summary>
    public VerseStoryRuntime StoryRuntime => _storyRuntime;

    /// <summary>
    /// 模块加载器
    /// </summary>
    public VerseModuleLoader ModuleLoader => _moduleLoader;

    /// <summary>
    /// 原生函数注册表
    /// </summary>
    public NativeFunctionRegistry NativeRegistry => _nativeRegistry;

    /// <summary>
    /// VM 解释器
    /// </summary>
    public VMInterpreter Interpreter => _interpreter;

    #endregion

    #region 构造函数

    public VerseRuntime()
    {
        _vmState = new VMState();
        _nativeRegistry = new NativeFunctionRegistry();
        _storyRuntime = new VerseStoryRuntime();
        _bridge = new VerseNativeBridge(_nativeRegistry, _storyRuntime);
        _moduleLoader = new VerseModuleLoader(_vmState, _bridge, _storyRuntime);
        _interpreter = new VMInterpreter(_vmState, _nativeRegistry);

        _bridge.RegisterAll();
    }

    public VerseRuntime(Gnosis.ECS.World.IWorld world) : this()
    {
        _interpreter.SetWorld(world);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 加载 Verse 字节码模块
    /// </summary>
    public bool LoadVerseModule(BytecodeUnit unit)
    {
        return _moduleLoader.LoadModule(unit);
    }

    /// <summary>
    /// 执行指定场景
    /// </summary>
    public void RunScene(string sceneName)
    {
        _storyRuntime.SwitchScene(sceneName);

        var module = _vmState.CurrentModule;
        if (module is not null)
        {
            _interpreter.Run();
        }
    }

    /// <summary>
    /// 执行单步
    /// </summary>
    public bool Step()
    {
        return _interpreter.Step();
    }

    /// <summary>
    /// 停止执行
    /// </summary>
    public void Stop()
    {
        _vmState.Reset();
    }

    /// <summary>
    /// 重置运行时
    /// </summary>
    public void Reset()
    {
        _vmState.Reset();
        _storyRuntime.Reset();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _bridge.UnregisterAll();
            _disposed = true;
        }
    }

    #endregion
}
