namespace Gnosis.Interpreter.VM;

/// <summary>
/// 虚拟机状态，管理执行上下文
/// </summary>
public class VMState : IVMState
{
    #region Fields

    private readonly VMStack _stack;
    private readonly List<IModule> _modules;
    private readonly MemoryManager _memoryManager;
    private int _ip;
    private IModule? _currentModule;

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化虚拟机状态
    /// </summary>
    public VMState()
    {
        _stack = new VMStack();
        _modules = [];
        _memoryManager = new MemoryManager();
        _ip = 0;
        _currentModule = null;
    }

    #endregion

    #region IVMState Properties

    /// <summary>
    /// 指令指针
    /// </summary>
    public int IP
    {
        get => _ip;
        set => _ip = value;
    }

    /// <summary>
    /// 栈指针
    /// </summary>
    public int SP
    {
        get => _stack.SP;
        set { }
    }

    /// <summary>
    /// 操作数栈快照
    /// </summary>
    public object?[] Stack
    {
        get
        {
            var snapshot = new object?[_stack.Count];
            for (var i = 0; i < _stack.Count; i++)
            {
                snapshot[i] = _stack.GetAt(i);
            }

            return snapshot;
        }
    }

    /// <summary>
    /// 已加载的模块列表
    /// </summary>
    public IReadOnlyList<IModule> Modules => _modules;

    /// <summary>
    /// 当前模块
    /// </summary>
    public IModule? CurrentModule => _currentModule;

    #endregion

    #region IVMState Methods

    /// <summary>
    /// 压入值到操作数栈
    /// </summary>
    public void Push(object? value)
    {
        _stack.Push(value);
    }

    /// <summary>
    /// 从操作数栈弹出值
    /// </summary>
    public object? Pop()
    {
        return _stack.Pop();
    }

    /// <summary>
    /// 查看栈顶值但不弹出
    /// </summary>
    public object? Peek()
    {
        return _stack.Peek();
    }

    /// <summary>
    /// 加载模块，如果是第一个模块则设为当前模块
    /// </summary>
    public void LoadModule(IModule module)
    {
        _modules.Add(module);

        if (_modules.Count == 1)
        {
            _currentModule = module;
        }
    }

    /// <summary>
    /// 卸载模块，如果移除的是当前模块则置空
    /// </summary>
    public void UnloadModule(string name)
    {
        var module = _modules.FirstOrDefault(m => m.Name == name);

        if (module is null)
        {
            return;
        }

        _modules.Remove(module);

        if (_currentModule == module)
        {
            _currentModule = null;
        }
    }

    /// <summary>
    /// 按名称查找模块
    /// </summary>
    public IModule? GetModule(string name)
    {
        return _modules.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// 重置虚拟机状态，IP 归零，清空栈，模块保留但重设当前模块
    /// </summary>
    public void Reset()
    {
        _ip = 0;
        _stack.Clear();
        _currentModule = _modules.Count > 0 ? _modules[0] : null;
    }

    #endregion

    #region Internal Access

    /// <summary>
    /// 内部栈访问
    /// </summary>
    public VMStack StackInternal => _stack;

    /// <summary>
    /// 内存管理器访问
    /// </summary>
    public MemoryManager MemoryManager => _memoryManager;

    /// <summary>
    /// 设置当前模块
    /// </summary>
    public void SetCurrentModule(IModule? module)
    {
        _currentModule = module;
    }

    #endregion
}
