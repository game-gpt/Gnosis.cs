using Gnosis.Core;
using Gnosis.ECS;
using Gnosis.ECS.Entity;
using Gnosis.ECS.World;
using Gnosis.IR.Instruction;

namespace Gnosis.Runtime.VM;

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
            #region 常量加载

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
            case OpCode.PushTrue:
                _state.Push(true);
                break;
            case OpCode.PushFalse:
                _state.Push(false);
                break;
            case OpCode.PushNull:
                _state.Push(null);
                break;

            #endregion

            #region 栈操作

            case OpCode.Pop:
                ExecutePop();
                break;
            case OpCode.Dup:
                ExecuteDup();
                break;

            #endregion

            #region 整数算术

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

            #endregion

            #region 浮点算术

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

            #endregion

            #region 整数比较

            case OpCode.EqualInt:
                ExecuteEqualInt();
                break;
            case OpCode.NotEqualInt:
                ExecuteNotEqualInt();
                break;
            case OpCode.LessInt:
                ExecuteLessInt();
                break;
            case OpCode.GreaterInt:
                ExecuteGreaterInt();
                break;
            case OpCode.LessEqualInt:
                ExecuteLessEqualInt();
                break;
            case OpCode.GreaterEqualInt:
                ExecuteGreaterEqualInt();
                break;

            #endregion

            #region 浮点比较

            case OpCode.EqualFloat:
                ExecuteEqualFloat();
                break;
            case OpCode.NotEqualFloat:
                ExecuteNotEqualFloat();
                break;
            case OpCode.LessFloat:
                ExecuteLessFloat();
                break;
            case OpCode.GreaterFloat:
                ExecuteGreaterFloat();
                break;
            case OpCode.LessEqualFloat:
                ExecuteLessEqualFloat();
                break;
            case OpCode.GreaterEqualFloat:
                ExecuteGreaterEqualFloat();
                break;

            #endregion

            #region 逻辑运算

            case OpCode.And:
                ExecuteAnd();
                break;
            case OpCode.Or:
                ExecuteOr();
                break;
            case OpCode.Not:
                ExecuteNot();
                break;

            #endregion

            #region 控制流

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

            #endregion

            #region 变量存取

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

            #endregion

            #region 对象操作

            case OpCode.NewObject:
                ExecuteNewObject();
                break;
            case OpCode.GetField:
                ExecuteGetField();
                break;
            case OpCode.SetField:
                ExecuteSetField();
                break;

            #endregion

            #region ECS 实体操作

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

            #endregion

            #region 字符串操作

            case OpCode.PushString:
                ExecutePushString();
                break;
            case OpCode.ConcatString:
                ExecuteConcatString();
                break;
            case OpCode.StringLength:
                ExecuteStringLength();
                break;
            case OpCode.StringGetChar:
                ExecuteStringGetChar();
                break;

            #endregion

            #region 数组操作

            case OpCode.NewArray:
                ExecuteNewArray();
                break;
            case OpCode.ArrayGet:
                ExecuteArrayGet();
                break;
            case OpCode.ArraySet:
                ExecuteArraySet();
                break;
            case OpCode.ArrayLength:
                ExecuteArrayLength();
                break;

            #endregion

            #region 闭包操作

            case OpCode.MakeClosure:
                ExecuteMakeClosure();
                break;
            case OpCode.GetUpvalue:
                ExecuteGetUpvalue();
                break;
            case OpCode.SetUpvalue:
                ExecuteSetUpvalue();
                break;

            #endregion

            #region 类型检查

            case OpCode.IsNull:
                ExecuteIsNull();
                break;
            case OpCode.IsType:
                ExecuteIsType();
                break;
            case OpCode.TypeOf:
                ExecuteTypeOf();
                break;

            #endregion

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

    #region 常量加载

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

    #region 栈操作

    private void ExecutePop()
    {
        _state.Pop();
    }

    private void ExecuteDup()
    {
        _state.StackInternal.Dup();
    }

    #endregion

    #region 整数算术

    private void ExecuteAddInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a + b);
    }

    private void ExecuteSubInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a - b);
    }

    private void ExecuteMulInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a * b);
    }

    private void ExecuteDivInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());

        if (b == 0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(a / b);
    }

    private void ExecuteNegInt()
    {
        var a = ToInt64(_state.Pop());
        _state.Push(-a);
    }

    #endregion

    #region 浮点算术

    private void ExecuteAddFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a + b);
    }

    private void ExecuteSubFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a - b);
    }

    private void ExecuteMulFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a * b);
    }

    private void ExecuteDivFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());

        if (b == 0.0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(a / b);
    }

    private void ExecuteNegFloat()
    {
        var a = ToFloat64(_state.Pop());
        _state.Push(-a);
    }

    #endregion

    #region 整数比较

    private void ExecuteEqualInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a == b);
    }

    private void ExecuteNotEqualInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a != b);
    }

    private void ExecuteLessInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a < b);
    }

    private void ExecuteGreaterInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a > b);
    }

    private void ExecuteLessEqualInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a <= b);
    }

    private void ExecuteGreaterEqualInt()
    {
        var b = ToInt64(_state.Pop());
        var a = ToInt64(_state.Pop());
        _state.Push(a >= b);
    }

    #endregion

    #region 浮点比较

    private void ExecuteEqualFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(Math.Abs(a - b) < double.Epsilon);
    }

    private void ExecuteNotEqualFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(Math.Abs(a - b) >= double.Epsilon);
    }

    private void ExecuteLessFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a < b);
    }

    private void ExecuteGreaterFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a > b);
    }

    private void ExecuteLessEqualFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a <= b);
    }

    private void ExecuteGreaterEqualFloat()
    {
        var b = ToFloat64(_state.Pop());
        var a = ToFloat64(_state.Pop());
        _state.Push(a >= b);
    }

    #endregion

    #region 逻辑运算

    private void ExecuteAnd()
    {
        var b = IsTruthy(_state.Pop());
        var a = IsTruthy(_state.Pop());
        _state.Push(a && b);
    }

    private void ExecuteOr()
    {
        var b = IsTruthy(_state.Pop());
        var a = IsTruthy(_state.Pop());
        _state.Push(a || b);
    }

    private void ExecuteNot()
    {
        var a = IsTruthy(_state.Pop());
        _state.Push(!a);
    }

    #endregion

    #region 控制流

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
        var paramCount = ReadInt32();
        var localCount = ReadInt32();

        var args = new object?[paramCount];
        for (var i = paramCount - 1; i >= 0; i--)
        {
            args[i] = _state.Pop();
        }

        _state.StackInternal.PushFrame(_state.IP, _state.StackInternal.SP, localCount);

        var frame = _state.StackInternal.CurrentFrame;
        if (frame.HasValue)
        {
            for (var i = 0; i < paramCount && i < frame.Value.Locals.Length; i++)
            {
                frame.Value.Locals[i] = args[i];
            }
        }

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

    #region 变量存取

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
        _state.Push(_state.GetGlobal(idx));
    }

    private void ExecuteStoreGlobal()
    {
        var idx = ReadInt32();
        var value = _state.Pop();
        _state.SetGlobal(idx, value);
    }

    private void ExecuteLoadField()
    {
        var idx = ReadInt32();
        var target = _state.Pop();

        if (target is long objectId)
        {
            var obj = _state.MemoryManager.GetObject<GGObject>((int)objectId);
            if (obj is not null)
            {
                var fieldName = $"field_{idx}";
                if (obj.HasField(fieldName))
                {
                    _state.Push(obj.GetField(fieldName));
                    return;
                }
            }
        }

        _state.Push(null);
    }

    private void ExecuteStoreField()
    {
        var idx = ReadInt32();
        var value = _state.Pop();
        var target = _state.Pop();

        if (target is long objectId)
        {
            var obj = _state.MemoryManager.GetObject<GGObject>((int)objectId);
            if (obj is not null)
            {
                obj.SetField($"field_{idx}", value);
            }
        }
    }

    #endregion

    #region 对象操作

    private void ExecuteNewObject()
    {
        var typeIdx = ReadInt32();
        var typeName = ReadConstant()?.ToString();
        var obj = new GGObject(typeName ?? "object");
        var objectId = _state.MemoryManager.Allocate(obj);
        _state.Push((long)objectId);
    }

    private void ExecuteGetField()
    {
        var fieldIdx = ReadInt32();
        var fieldName = ReadConstant()?.ToString() ?? $"field_{fieldIdx}";
        var target = _state.Pop();

        if (target is long objectId)
        {
            var obj = _state.MemoryManager.GetObject<GGObject>((int)objectId);

            if (obj is not null && obj.HasField(fieldName))
            {
                _state.Push(obj.GetField(fieldName));
                return;
            }
        }

        _state.Push(null);
    }

    private void ExecuteSetField()
    {
        var fieldIdx = ReadInt32();
        var fieldName = ReadConstant()?.ToString() ?? $"field_{fieldIdx}";
        var value = _state.Pop();
        var target = _state.Pop();

        if (target is long objectId)
        {
            var obj = _state.MemoryManager.GetObject<GGObject>((int)objectId);

            if (obj is not null)
            {
                obj.SetField(fieldName, value);
            }
        }
    }

    #endregion

    #region ECS 实体操作

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

    #region 字符串操作

    private void ExecutePushString()
    {
        var idx = ReadInt32();

        if (_state.CurrentModule?.Constants.TryGetValue(idx.ToString(), out var value) == true)
        {
            _state.Push(value?.ToString());
        }
        else
        {
            _state.Push(null);
        }
    }

    private void ExecuteConcatString()
    {
        var b = _state.Pop()?.ToString() ?? "";
        var a = _state.Pop()?.ToString() ?? "";
        _state.Push(string.Concat(a, b));
    }

    private void ExecuteStringLength()
    {
        var str = _state.Pop()?.ToString() ?? "";
        _state.Push((long)str.Length);
    }

    private void ExecuteStringGetChar()
    {
        var idx = ToInt32(_state.Pop());
        var str = _state.Pop()?.ToString() ?? "";

        if (idx >= 0 && idx < str.Length)
        {
            _state.Push(str[idx].ToString());
        }
        else
        {
            _state.Push(null);
        }
    }

    #endregion

    #region 数组操作

    private void ExecuteNewArray()
    {
        var size = ReadInt32();
        var arr = new GGArray(size);
        var objectId = _state.MemoryManager.Allocate(arr);
        _state.Push((long)objectId);
    }

    private void ExecuteArrayGet()
    {
        var index = ToInt32(_state.Pop());
        var target = _state.Pop();

        if (target is long objectId)
        {
            var arr = _state.MemoryManager.GetObject<GGArray>((int)objectId);

            if (arr is not null && index >= 0 && index < arr.Count)
            {
                _state.Push(arr[index]);
                return;
            }
        }

        _state.Push(null);
    }

    private void ExecuteArraySet()
    {
        var value = _state.Pop();
        var index = ToInt32(_state.Pop());
        var target = _state.Pop();

        if (target is long objectId)
        {
            var arr = _state.MemoryManager.GetObject<GGArray>((int)objectId);

            if (arr is not null && index >= 0 && index < arr.Capacity)
            {
                arr[index] = value;
            }
        }
    }

    private void ExecuteArrayLength()
    {
        var target = _state.Pop();

        if (target is long objectId)
        {
            var arr = _state.MemoryManager.GetObject<GGArray>((int)objectId);

            if (arr is not null)
            {
                _state.Push((long)arr.Count);
                return;
            }
        }

        _state.Push(0L);
    }

    #endregion

    #region 闭包操作

    private void ExecuteMakeClosure()
    {
        var funcAddr = ReadInt32();
        var closure = new GGClosure(funcAddr, 0);
        var objectId = _state.MemoryManager.Allocate(closure);
        _state.Push((long)objectId);
    }

    private void ExecuteGetUpvalue()
    {
        var idx = ToInt32(_state.Pop());
        var target = _state.Pop();

        if (target is long objectId)
        {
            var closure = _state.MemoryManager.GetObject<GGClosure>((int)objectId);

            if (closure is not null)
            {
                _state.Push(closure.GetUpvalue(idx));
                return;
            }
        }

        _state.Push(null);
    }

    private void ExecuteSetUpvalue()
    {
        var value = _state.Pop();
        var idx = ToInt32(_state.Pop());
        var target = _state.Pop();

        if (target is long objectId)
        {
            var closure = _state.MemoryManager.GetObject<GGClosure>((int)objectId);

            if (closure is not null)
            {
                closure.SetUpvalue(idx, value);
            }
        }
    }

    #endregion

    #region 类型检查

    private void ExecuteIsNull()
    {
        var value = _state.Pop();
        _state.Push(value is null);
    }

    private void ExecuteIsType()
    {
        var typeIdx = ReadInt32();
        var typeName = ReadConstant()?.ToString() ?? "";
        var value = _state.Pop();

        bool result = typeName switch
        {
            "int" or "i32" or "i64" => value is long,
            "float" or "f32" or "f64" => value is double,
            "bool" => value is bool,
            "string" => value is GGString,
            "array" => value is long id && _state.MemoryManager.GetObject<GGArray>((int)id) is not null,
            "object" => value is long id2 && _state.MemoryManager.GetObject<GGObject>((int)id2) is not null,
            _ => false
        };

        _state.Push(result);
    }

    private void ExecuteTypeOf()
    {
        var typeIdx = ReadInt32();
        var value = _state.Pop();

        string typeName = value switch
        {
            null => "null",
            long => "int",
            double => "float",
            bool => "bool",
            GGString => "string",
            _ => "unknown"
        };

        _state.Push((long)typeName.GetHashCode());
    }

    #endregion

    #region 辅助方法

    private byte ReadByte()
    {
        return _instructions![_state.IP++];
    }

    private short ReadInt16()
    {
        var value = BitConverter.ToInt16(_instructions!, _state.IP);
        _state.IP += 2;
        return value;
    }

    private int ReadInt32()
    {
        var value = BitConverter.ToInt32(_instructions!, _state.IP);
        _state.IP += 4;
        return value;
    }

    private long ReadInt64()
    {
        var value = BitConverter.ToInt64(_instructions!, _state.IP);
        _state.IP += 8;
        return value;
    }

    private float ReadFloat32()
    {
        var value = BitConverter.ToSingle(_instructions!, _state.IP);
        _state.IP += 4;
        return value;
    }

    private double ReadFloat64()
    {
        var value = BitConverter.ToDouble(_instructions!, _state.IP);
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

    private static long ToInt64(object? value)
    {
        return value switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            double d => (long)d,
            float f => (long)f,
            bool bo => bo ? 1 : 0,
            _ => 0
        };
    }

    private static int ToInt32(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            byte b => b,
            double d => (int)d,
            float f => (int)f,
            bool bo => bo ? 1 : 0,
            _ => 0
        };
    }

    private static double ToFloat64(object? value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            bool bo => bo ? 1.0 : 0.0,
            _ => 0.0
        };
    }

    #endregion
}
