using System.Runtime.CompilerServices;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 调用帧，保存函数调用的返回信息
/// </summary>
public struct CallFrame
{
    /// <summary>
    /// 返回地址
    /// </summary>
    public int ReturnAddress;

    /// <summary>
    /// 基址指针
    /// </summary>
    public int BasePointer;

    /// <summary>
    /// 局部变量表
    /// </summary>
    public GGValue[] Locals;

    /// <summary>
    /// 返回时需要切换回的模块名称，为 null 表示同模块返回
    /// </summary>
    public string? ReturnModuleName;

    /// <summary>
    /// 初始化调用帧
    /// </summary>
    public CallFrame(int returnAddress, int basePointer, int localCount, string? returnModuleName = null)
    {
        ReturnAddress = returnAddress;
        BasePointer = basePointer;
        Locals = new GGValue[localCount];
        ReturnModuleName = returnModuleName;
    }
}

/// <summary>
/// 虚拟机栈，管理操作数栈和调用帧栈
/// </summary>
public class VMStack
{
    #region Fields

    private GGValue[] _operandStack;
    private int _sp;
    private readonly List<CallFrame> _callFrames;
    private const int DefaultStackSize = 1024;

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化虚拟机栈
    /// </summary>
    public VMStack(int size = DefaultStackSize)
    {
        _operandStack = new GGValue[size];
        _sp = 0;
        _callFrames = [];
    }

    #endregion

    #region Properties

    /// <summary>
    /// 栈指针
    /// </summary>
    public int SP => _sp;

    /// <summary>
    /// 操作数栈中元素数量
    /// </summary>
    public int Count => _sp;

    /// <summary>
    /// 当前调用帧
    /// </summary>
    public CallFrame? CurrentFrame => _callFrames.Count > 0 ? _callFrames[^1] : null;

    /// <summary>
    /// 调用帧数量
    /// </summary>
    public int FrameCount => _callFrames.Count;

    /// <summary>
    /// 内部调用帧列表访问
    /// </summary>
    public IReadOnlyList<CallFrame> FramesInternal => _callFrames;

    #endregion

    #region Operand Stack Operations

    /// <summary>
    /// 压入值到操作数栈
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(GGValue value)
    {
        if (_sp >= _operandStack.Length)
        {
            Array.Resize(ref _operandStack, _operandStack.Length * 2);
        }

        _operandStack[_sp++] = value;
    }

    /// <summary>
    /// 从操作数栈弹出值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GGValue Pop()
    {
        if (_sp <= 0)
        {
            throw new VMStackUnderflowException();
        }

        var value = _operandStack[--_sp];
        _operandStack[_sp] = default;
        return value;
    }

    /// <summary>
    /// 查看栈顶值但不弹出
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GGValue Peek()
    {
        if (_sp <= 0)
        {
            throw new VMStackUnderflowException();
        }

        return _operandStack[_sp - 1];
    }

    /// <summary>
    /// 复制栈顶值并压入
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dup()
    {
        if (_sp <= 0)
        {
            throw new VMStackUnderflowException();
        }

        Push(_operandStack[_sp - 1]);
    }

    /// <summary>
    /// 获取指定索引的值（相对于栈底）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GGValue GetAt(int index)
    {
        if (index < 0 || index >= _sp)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出操作数栈范围");
        }

        return _operandStack[index];
    }

    /// <summary>
    /// 设置指定索引的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAt(int index, GGValue value)
    {
        if (index < 0 || index >= _sp)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出操作数栈范围");
        }

        _operandStack[index] = value;
    }

    /// <summary>
    /// 获取操作数栈内部数组的直接引用（用于 GC Roots 收集）
    /// </summary>
    public GGValue[] GetRawStack() => _operandStack;

    #endregion

    #region Call Frame Operations

    /// <summary>
    /// 压入调用帧
    /// </summary>
    public void PushFrame(int returnAddress, int basePointer, int localCount)
    {
        _callFrames.Add(new CallFrame(returnAddress, basePointer, localCount));
    }

    /// <summary>
    /// 压入调用帧（带返回模块名称）
    /// </summary>
    public void PushFrame(int returnAddress, int basePointer, int localCount, string? returnModuleName)
    {
        _callFrames.Add(new CallFrame(returnAddress, basePointer, localCount, returnModuleName));
    }

    /// <summary>
    /// 弹出调用帧
    /// </summary>
    public CallFrame PopFrame()
    {
        if (_callFrames.Count <= 0)
        {
            throw new VMStackUnderflowException("调用帧栈为空");
        }

        var frame = _callFrames[^1];
        _callFrames.RemoveAt(_callFrames.Count - 1);
        return frame;
    }

    #endregion

    #region Clear

    /// <summary>
    /// 清空操作数栈和调用帧栈
    /// </summary>
    public void Clear()
    {
        Array.Clear(_operandStack, 0, _sp);
        _sp = 0;
        _callFrames.Clear();
    }

    #endregion
}
