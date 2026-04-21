using Gnosis.ECS;
using Gnosis.ECS.Core;
using Gnosis.ECS.Interface;
using Gnosis.Interpreter.IR;

namespace Gnosis.Interpreter.VM;

/// <summary>
/// 虚拟机解释器，执行字节码指令
/// </summary>
public class VMInterpreter
{
    #region Fields

    private readonly VMState _state;
    private readonly NativeFunctionRegistry _nativeRegistry;
    private IWorld? _world;
    private bool _running;
    private byte[]? _instructions;

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化虚拟机解释器
    /// </summary>
    public VMInterpreter(VMState state, NativeFunctionRegistry nativeRegistry)
    {
        _state = state;
        _nativeRegistry = nativeRegistry;
        _world = null;
        _running = false;
        _instructions = null;
    }

    /// <summary>
    /// 初始化虚拟机解释器并绑定 ECS 世界
    /// </summary>
    public VMInterpreter(VMState state, NativeFunctionRegistry nativeRegistry, IWorld world)
    {
        _state = state;
        _nativeRegistry = nativeRegistry;
        _world = world;
        _running = false;
        _instructions = null;
    }

    #endregion

    #region Properties

    /// <summary>
    /// 虚拟机状态
    /// </summary>
    public VMState State => _state;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _running;

    #endregion

    #region Public Methods

    /// <summary>
    /// 运行直到 Halt 或错误
    /// </summary>
    public void Run()
    {
        if (_state.CurrentModule is null)
        {
            throw new VMModuleNotFoundException("未加载任何模块");
        }

        _instructions = _state.CurrentModule.Instructions.ToArray();
        _running = true;

        while (_running)
        {
            if (_state.IP < 0 || _state.IP >= _instructions.Length)
            {
                break;
            }

            if (!Step())
            {
                break;
            }
        }

        _running = false;
    }

    /// <summary>
    /// 执行单条指令，返回是否继续
    /// </summary>
    public bool Step()
    {
        if (_instructions is null || _state.IP < 0 || _state.IP >= _instructions.Length)
        {
            return false;
        }

        var opCode = (OpCode)ReadByte();

        switch (opCode)
        {
            case OpCode.PushInt8:
                ExecutePushInt8();
                break;
            case OpCode.PushInt16:
                ExecutePushInt16();
                break;
            case OpCode.PushInt32:
                ExecutePushInt32();
                break;
            case OpCode.PushInt64:
                ExecutePushInt64();
                break;
            case OpCode.PushFloat32:
                ExecutePushFloat32();
                break;
            case OpCode.PushFloat64:
                ExecutePushFloat64();
                break;

            case OpCode.Pop:
                ExecutePop();
                break;
            case OpCode.Dup:
                ExecuteDup();
                break;

            case OpCode.AddInt:
                ExecuteAddInt();
                break;
            case OpCode.SubInt:
                ExecuteSubInt();
                break;
            case OpCode.MulInt:
                ExecuteMulInt();
                break;
            case OpCode.DivInt:
                ExecuteDivInt();
                break;
            case OpCode.NegInt:
                ExecuteNegInt();
                break;
            case OpCode.AddFloat:
                ExecuteAddFloat();
                break;
            case OpCode.SubFloat:
                ExecuteSubFloat();
                break;
            case OpCode.MulFloat:
                ExecuteMulFloat();
                break;
            case OpCode.DivFloat:
                ExecuteDivFloat();
                break;
            case OpCode.NegFloat:
                ExecuteNegFloat();
                break;

            case OpCode.Jump:
                ExecuteJump();
                break;
            case OpCode.JumpIfTrue:
                ExecuteJumpIfTrue();
                break;
            case OpCode.JumpIfFalse:
                ExecuteJumpIfFalse();
                break;
            case OpCode.Call:
                ExecuteCall();
                break;
            case OpCode.CallNative:
                ExecuteCallNative();
                break;
            case OpCode.Return:
                ExecuteReturn();
                break;
            case OpCode.Halt:
                _running = false;
                return false;
            case OpCode.Nop:
                break;

            case OpCode.LoadLocal:
                ExecuteLoadLocal();
                break;
            case OpCode.StoreLocal:
                ExecuteStoreLocal();
                break;
            case OpCode.LoadGlobal:
                ExecuteLoadGlobal();
                break;
            case OpCode.StoreGlobal:
                ExecuteStoreGlobal();
                break;
            case OpCode.LoadField:
                ExecuteLoadField();
                break;
            case OpCode.StoreField:
                ExecuteStoreField();
                break;

            case OpCode.NewObject:
                ExecuteNewObject();
                break;
            case OpCode.GetField:
                ExecuteGetField();
                break;
            case OpCode.SetField:
                ExecuteSetField();
                break;

            case OpCode.SpawnEntity:
                ExecuteSpawnEntity();
                break;
            case OpCode.DestroyEntity:
                ExecuteDestroyEntity();
                break;
            case OpCode.AddComponent:
                ExecuteAddComponent();
                break;
            case OpCode.GetComponent:
                ExecuteGetComponent();
                break;
            case OpCode.RemoveComponent:
                ExecuteRemoveComponent();
                break;
            case OpCode.QueryAll:
                ExecuteQueryAll();
                break;
            case OpCode.QueryAny:
                ExecuteQueryAny();
                break;

            default:
                throw new VMUnknownOpCodeException((byte)opCode);
        }

        return true;
    }

    /// <summary>
    /// 设置 ECS 世界
    /// </summary>
    public void SetWorld(IWorld world)
    {
        _world = world;
    }

    #endregion

    #region Constant Loading

    private void ExecutePushInt8()
    {
        var value = ReadByte();
        _state.Push((long)value);
    }

    private void ExecutePushInt16()
    {
        var value = ReadInt16();
        _state.Push((long)value);
    }

    private void ExecutePushInt32()
    {
        var value = ReadInt32();
        _state.Push((long)value);
    }

    private void ExecutePushInt64()
    {
        var value = ReadInt64();
        _state.Push(value);
    }

    private void ExecutePushFloat32()
    {
        var value = ReadFloat32();
        _state.Push((double)value);
    }

    private void ExecutePushFloat64()
    {
        var value = ReadFloat64();
        _state.Push(value);
    }

    #endregion

    #region Stack Operations

    private void ExecutePop()
    {
        _state.Pop();
    }

    private void ExecuteDup()
    {
        _state.StackInternal.Dup();
    }

    #endregion

    #region Arithmetic

    private void ExecuteAddInt()
    {
        var b = (long)_state.Pop()!;
        var a = (long)_state.Pop()!;
        _state.Push(b + a);
    }

    private void ExecuteSubInt()
    {
        var b = (long)_state.Pop()!;
        var a = (long)_state.Pop()!;
        _state.Push(a - b);
    }

    private void ExecuteMulInt()
    {
        var b = (long)_state.Pop()!;
        var a = (long)_state.Pop()!;
        _state.Push(a * b);
    }

    private void ExecuteDivInt()
    {
        var b = (long)_state.Pop()!;
        var a = (long)_state.Pop()!;

        if (b == 0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(a / b);
    }

    private void ExecuteNegInt()
    {
        var a = (long)_state.Pop()!;
        _state.Push(-a);
    }

    private void ExecuteAddFloat()
    {
        var b = (double)_state.Pop()!;
        var a = (double)_state.Pop()!;
        _state.Push(b + a);
    }

    private void ExecuteSubFloat()
    {
        var b = (double)_state.Pop()!;
        var a = (double)_state.Pop()!;
        _state.Push(a - b);
    }

    private void ExecuteMulFloat()
    {
        var b = (double)_state.Pop()!;
        var a = (double)_state.Pop()!;
        _state.Push(a * b);
    }

    private void ExecuteDivFloat()
    {
        var b = (double)_state.Pop()!;
        var a = (double)_state.Pop()!;

        if (b == 0.0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(a / b);
    }

    private void ExecuteNegFloat()
    {
        var a = (double)_state.Pop()!;
        _state.Push(-a);
    }

    #endregion

    #region Control Flow

    private void ExecuteJump()
    {
        _state.IP = ReadInt32();
    }

    private void ExecuteJumpIfTrue()
    {
        var target = ReadInt32();

        if (IsTruthy(_state.Pop()))
        {
            _state.IP = target;
        }
    }

    private void ExecuteJumpIfFalse()
    {
        var target = ReadInt32();

        if (!IsTruthy(_state.Pop()))
        {
            _state.IP = target;
        }
    }

    private void ExecuteCall()
    {
        var addr = ReadInt32();
        _state.StackInternal.PushFrame(_state.IP, _state.StackInternal.SP, 0);
        _state.IP = addr;
    }

    private void ExecuteCallNative()
    {
        var funcId = ReadInt32();
        var func = _nativeRegistry.Get(funcId);

        if (func is not null)
        {
            var args = new object?[func.ParameterCount];

            for (var i = func.ParameterCount - 1; i >= 0; i--)
            {
                args[i] = _state.Pop();
            }

            func.Execute(_state, args);
        }
        else
        {
            _state.Push(null);
        }
    }

    private void ExecuteReturn()
    {
        var frame = _state.StackInternal.PopFrame();
        _state.IP = frame.ReturnAddress;
    }

    #endregion

    #region Variable Access

    private void ExecuteLoadLocal()
    {
        var idx = ReadInt32();
        var frame = _state.StackInternal.CurrentFrame;

        if (frame.HasValue && idx < frame.Value.Locals.Length)
        {
            _state.Push(frame.Value.Locals[idx]);
        }
        else
        {
            _state.Push(null);
        }
    }

    private void ExecuteStoreLocal()
    {
        var idx = ReadInt32();
        var value = _state.Pop();
        var frame = _state.StackInternal.CurrentFrame;

        if (frame.HasValue && idx < frame.Value.Locals.Length)
        {
            frame.Value.Locals[idx] = value;
        }
    }

    private void ExecuteLoadGlobal()
    {
        var idx = ReadInt32();

        if (_state.CurrentModule?.Constants.TryGetValue(idx.ToString(), out var value) == true)
        {
            _state.Push(value);
        }
        else
        {
            _state.Push(null);
        }
    }

    private void ExecuteStoreGlobal()
    {
        var idx = ReadInt32();
        _state.Pop();
    }

    private void ExecuteLoadField()
    {
        var idx = ReadInt32();
        _state.Pop();
        _state.Push(null);
    }

    private void ExecuteStoreField()
    {
        var idx = ReadInt32();
        _state.Pop();
        _state.Pop();
    }

    #endregion

    #region Object Operations

    private void ExecuteNewObject()
    {
        var typeIdx = ReadInt32();
        var typeName = ReadConstant()?.ToString();
        var objectId = _state.MemoryManager.Allocate(typeName ?? "");
        _state.Push((long)objectId);
    }

    private void ExecuteGetField()
    {
        var fieldIdx = ReadInt32();
        _state.Pop();
        _state.Push(null);
    }

    private void ExecuteSetField()
    {
        var fieldIdx = ReadInt32();
        _state.Pop();
        _state.Pop();
    }

    #endregion

    #region Entity Operations

    private void ExecuteSpawnEntity()
    {
        if (_world is not null)
        {
            var id = _world.CreateEntity();
            _state.Push(id);
        }
        else
        {
            _state.Push(0L);
        }
    }

    private void ExecuteDestroyEntity()
    {
        if (_world is not null)
        {
            var value = _state.Pop();

            if (value is EntityId entityId)
            {
                _world.DestroyEntity(entityId);
            }
        }
        else
        {
            _state.Pop();
        }
    }

    private void ExecuteAddComponent()
    {
        var typeIdx = ReadInt32();
        _state.Pop();
    }

    private void ExecuteGetComponent()
    {
        var typeIdx = ReadInt32();
        _state.Pop();
        _state.Push(null);
    }

    private void ExecuteRemoveComponent()
    {
        var typeIdx = ReadInt32();
        _state.Pop();
    }

    private void ExecuteQueryAll()
    {
        var count = ReadInt32();

        for (var i = 0; i < count; i++)
        {
            ReadInt32();
        }

        _state.Push(0L);
    }

    private void ExecuteQueryAny()
    {
        var count = ReadInt32();

        for (var i = 0; i < count; i++)
        {
            ReadInt32();
        }

        _state.Push(0L);
    }

    #endregion

    #region Helper Methods

    private byte ReadByte()
    {
        return _instructions![_state.IP++];
    }

    private short ReadInt16()
    {
        var value = BitConverter.ToInt16(_instructions, _state.IP);
        _state.IP += 2;
        return value;
    }

    private int ReadInt32()
    {
        var value = BitConverter.ToInt32(_instructions, _state.IP);
        _state.IP += 4;
        return value;
    }

    private long ReadInt64()
    {
        var value = BitConverter.ToInt64(_instructions, _state.IP);
        _state.IP += 8;
        return value;
    }

    private float ReadFloat32()
    {
        var value = BitConverter.ToSingle(_instructions, _state.IP);
        _state.IP += 4;
        return value;
    }

    private double ReadFloat64()
    {
        var value = BitConverter.ToDouble(_instructions, _state.IP);
        _state.IP += 8;
        return value;
    }

    private object? ReadConstant()
    {
        var idx = ReadInt32();

        if (_state.CurrentModule?.Constants.TryGetValue(idx.ToString(), out var value) == true)
        {
            return value;
        }

        return null;
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            0L => false,
            0 => false,
            false => false,
            0.0 => false,
            "" => false,
            _ => true
        };
    }

    #endregion
}
