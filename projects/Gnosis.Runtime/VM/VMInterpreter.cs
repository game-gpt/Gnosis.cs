using System.Runtime.CompilerServices;
using Gnosis.Core;
using Gnosis.ECS;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Query;
using Gnosis.ECS.System;
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
    private readonly ComponentTypeRegistry _componentRegistry;
    private readonly ScriptSystemScheduler _scriptSystemScheduler;
    private IWorld? _world;
    private bool _running;
    private byte[]? _instructions;
    private int _nextSystemIndex;

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化虚拟机解释器
    /// </summary>
    public VMInterpreter(VMState state, NativeFunctionRegistry nativeRegistry)
    {
        _state = state;
        _nativeRegistry = nativeRegistry;
        _componentRegistry = new ComponentTypeRegistry();
        _scriptSystemScheduler = new ScriptSystemScheduler();
        _world = null;
        _running = false;
        _instructions = null;
        _nextSystemIndex = 0;
    }

    /// <summary>
    /// 初始化虚拟机解释器并绑定 ECS 世界
    /// </summary>
    public VMInterpreter(VMState state, NativeFunctionRegistry nativeRegistry, IWorld world)
    {
        _state = state;
        _nativeRegistry = nativeRegistry;
        _componentRegistry = new ComponentTypeRegistry();
        _scriptSystemScheduler = new ScriptSystemScheduler();
        _world = world;
        _running = false;
        _instructions = null;
        _nextSystemIndex = 0;
    }

    /// <summary>
    /// 初始化虚拟机解释器，绑定 ECS 世界和组件类型注册表
    /// </summary>
    public VMInterpreter(VMState state, NativeFunctionRegistry nativeRegistry,
        ComponentTypeRegistry componentRegistry, IWorld world)
    {
        _state = state;
        _nativeRegistry = nativeRegistry;
        _componentRegistry = componentRegistry;
        _scriptSystemScheduler = new ScriptSystemScheduler();
        _world = world;
        _running = false;
        _instructions = null;
        _nextSystemIndex = 0;
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

    /// <summary>
    /// 组件类型注册表
    /// </summary>
    public ComponentTypeRegistry ComponentRegistry => _componentRegistry;

    /// <summary>
    /// 脚本系统调度器
    /// </summary>
    public ScriptSystemScheduler ScriptSystemScheduler => _scriptSystemScheduler;

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
                _state.Push(GGValue.FromBool(true));
                break;
            case OpCode.PushFalse:
                _state.Push(GGValue.FromBool(false));
                break;
            case OpCode.PushNull:
                _state.Push(GGValue.Null);
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
            case OpCode.SetComponent:
                ExecuteSetComponent();
                break;
            case OpCode.RemoveComponent:
                ExecuteRemoveComponent();
                break;
            case OpCode.HasComponent:
                ExecuteHasComponent();
                break;
            case OpCode.QueryAll:
                ExecuteQueryAll();
                break;
            case OpCode.QueryAny:
                ExecuteQueryAny();
                break;
            case OpCode.QueryWith:
                ExecuteQueryWith();
                break;
            case OpCode.QueryWithout:
                ExecuteQueryWithout();
                break;
            case OpCode.DefineComponent:
                ExecuteDefineComponent();
                break;
            case OpCode.DefineSystem:
                ExecuteDefineSystem();
                break;
            case OpCode.SystemSchedule:
                ExecuteSystemSchedule();
                break;
            case OpCode.WorldUpdate:
                ExecuteWorldUpdate();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushInt8()
    {
        var value = ReadByte();
        _state.Push(GGValue.FromInt(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushInt16()
    {
        var value = ReadInt16();
        _state.Push(GGValue.FromInt(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushInt32()
    {
        var value = ReadInt32();
        _state.Push(GGValue.FromInt(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushInt64()
    {
        var value = ReadInt64();
        _state.Push(GGValue.FromInt(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushFloat32()
    {
        var value = ReadFloat32();
        _state.Push(GGValue.FromFloat(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePushFloat64()
    {
        var value = ReadFloat64();
        _state.Push(GGValue.FromFloat(value));
    }

    #endregion

    #region 栈操作

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePop()
    {
        _state.Pop();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteDup()
    {
        _state.StackInternal.Dup();
    }

    #endregion

    #region 整数算术

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteAddInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromInt(a + b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteSubInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromInt(a - b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteMulInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromInt(a * b));
    }

    private void ExecuteDivInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;

        if (b == 0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(GGValue.FromInt(a / b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteNegInt()
    {
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromInt(-a));
    }

    #endregion

    #region 浮点算术

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteAddFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromFloat(a + b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteSubFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromFloat(a - b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteMulFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromFloat(a * b));
    }

    private void ExecuteDivFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;

        if (b == 0.0)
        {
            throw new VMDivideByZeroException();
        }

        _state.Push(GGValue.FromFloat(a / b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteNegFloat()
    {
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromFloat(-a));
    }

    #endregion

    #region 整数比较

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteEqualInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a == b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteNotEqualInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a != b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteLessInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a < b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteGreaterInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a > b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteLessEqualInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a <= b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteGreaterEqualInt()
    {
        var b = _state.Pop().IntValue;
        var a = _state.Pop().IntValue;
        _state.Push(GGValue.FromBool(a >= b));
    }

    #endregion

    #region 浮点比较

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteEqualFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(Math.Abs(a - b) < double.Epsilon));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteNotEqualFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(Math.Abs(a - b) >= double.Epsilon));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteLessFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(a < b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteGreaterFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(a > b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteLessEqualFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(a <= b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteGreaterEqualFloat()
    {
        var b = _state.Pop().FloatValue;
        var a = _state.Pop().FloatValue;
        _state.Push(GGValue.FromBool(a >= b));
    }

    #endregion

    #region 逻辑运算

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteAnd()
    {
        var b = _state.Pop().IsTruthy();
        var a = _state.Pop().IsTruthy();
        _state.Push(GGValue.FromBool(a && b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteOr()
    {
        var b = _state.Pop().IsTruthy();
        var a = _state.Pop().IsTruthy();
        _state.Push(GGValue.FromBool(a || b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteNot()
    {
        var a = _state.Pop().IsTruthy();
        _state.Push(GGValue.FromBool(!a));
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

        if (_state.Pop().IsTruthy())
        {
            _state.IP = target;
        }
    }

    private void ExecuteJumpIfFalse()
    {
        var target = ReadInt32();

        if (!_state.Pop().IsTruthy())
        {
            _state.IP = target;
        }
    }

    private void ExecuteCall()
    {
        var addr = ReadInt32();
        var paramCount = ReadInt32();
        var localCount = ReadInt32();

        var args = new GGValue[paramCount];
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
            var args = new GGValue[func.ParameterCount];

            for (var i = func.ParameterCount - 1; i >= 0; i--)
            {
                args[i] = _state.Pop();
            }

            var result = func.Execute(_state, args);
            _state.Push(result);
        }
        else
        {
            _state.Push(GGValue.Null);
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
            _state.Push(GGValue.Null);
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

        if (TryGetHeapObject<GGObject>(target, out var obj))
        {
            var fieldName = $"field_{idx}";
            if (obj.HasField(fieldName))
            {
                _state.Push(obj.GetField(fieldName));
                return;
            }
        }

        _state.Push(GGValue.Null);
    }

    private void ExecuteStoreField()
    {
        var idx = ReadInt32();
        var value = _state.Pop();
        var target = _state.Pop();

        if (TryGetHeapObject<GGObject>(target, out var obj))
        {
            obj.SetField($"field_{idx}", value);
        }
    }

    #endregion

    #region 对象操作

    private void ExecuteNewObject()
    {
        var typeIdx = ReadInt32();
        var typeName = ReadConstant()?.ToString();
        var obj = new GGObject(typeName ?? "object");
        _state.MemoryManager.Allocate(obj);
        _state.Push(GGValue.FromObject(obj));
    }

    private void ExecuteGetField()
    {
        var fieldIdx = ReadInt32();
        var fieldName = ReadConstant()?.ToString() ?? $"field_{fieldIdx}";
        var target = _state.Pop();

        if (TryGetHeapObject<GGObject>(target, out var obj) && obj.HasField(fieldName))
        {
            _state.Push(obj.GetField(fieldName));
            return;
        }

        _state.Push(GGValue.Null);
    }

    private void ExecuteSetField()
    {
        var fieldIdx = ReadInt32();
        var fieldName = ReadConstant()?.ToString() ?? $"field_{fieldIdx}";
        var value = _state.Pop();
        var target = _state.Pop();

        if (TryGetHeapObject<GGObject>(target, out var obj))
        {
            obj.SetField(fieldName, value);
        }
    }

    #endregion

    #region ECS 实体操作

    private void ExecuteSpawnEntity()
    {
        if (_world is not null)
        {
            var id = _world.CreateEntity();
            _state.Push(GGValue.FromEntity(id));
        }
        else
        {
            _state.Push(GGValue.FromEntity(EntityId.Null));
        }
    }

    private void ExecuteDestroyEntity()
    {
        if (_world is not null)
        {
            var value = _state.Pop();
            var entity = ResolveEntityId(value);
            _world.DestroyEntity(entity);
        }
        else
        {
            _state.Pop();
        }
    }

    private void ExecuteAddComponent()
    {
        var typeIdx = ReadInt32();
        var entityValue = _state.Pop();

        if (_world is null)
        {
            return;
        }

        var entity = ResolveEntityId(entityValue);
        _componentRegistry.AddComponent(_world, entity, typeIdx);
    }

    private void ExecuteGetComponent()
    {
        var typeIdx = ReadInt32();
        var entityValue = _state.Pop();

        if (_world is null)
        {
            _state.Push(GGValue.Null);
            return;
        }

        var entity = ResolveEntityId(entityValue);
        var result = _componentRegistry.GetComponent(_world, entity, typeIdx);
        _state.Push(result);
    }

    private void ExecuteSetComponent()
    {
        var typeIdx = ReadInt32();
        var dataValue = _state.Pop();
        var entityValue = _state.Pop();

        if (_world is null)
        {
            return;
        }

        var entity = ResolveEntityId(entityValue);

        if (TryGetHeapObject<GGStruct>(dataValue, out var data))
        {
            _componentRegistry.SetComponent(_world, entity, typeIdx, data);
        }
    }

    private void ExecuteRemoveComponent()
    {
        var typeIdx = ReadInt32();
        var entityValue = _state.Pop();

        if (_world is null)
        {
            return;
        }

        var entity = ResolveEntityId(entityValue);
        _componentRegistry.RemoveComponent(_world, entity, typeIdx);
    }

    private void ExecuteHasComponent()
    {
        var typeIdx = ReadInt32();
        var entityValue = _state.Pop();

        if (_world is null)
        {
            _state.Push(GGValue.FromBool(false));
            return;
        }

        var entity = ResolveEntityId(entityValue);
        var result = _componentRegistry.HasComponent(_world, entity, typeIdx);
        _state.Push(GGValue.FromBool(result));
    }

    private void ExecuteQueryAll()
    {
        var count = ReadInt32();
        var typeIndices = new int[count];

        for (var i = 0; i < count; i++)
        {
            typeIndices[i] = ReadInt32();
        }

        if (_world is null)
        {
            _state.Push(GGValue.Null);
            return;
        }

        var scriptIndices = typeIndices.Where(idx => _componentRegistry.IsScriptComponent(idx)).ToArray();
        var nativeIndices = typeIndices.Where(idx => !_componentRegistry.IsScriptComponent(idx)).ToArray();

        List<EntityId> results;

        if (scriptIndices.Length > 0 && nativeIndices.Length > 0)
        {
            var scriptResults = _componentRegistry.ScriptStorage.QueryAll(scriptIndices);
            var query = _world.CreateQuery();
            foreach (var typeIdx in nativeIndices)
            {
                var clrType = _componentRegistry.GetClrType(typeIdx);
                if (clrType is not null && query is EntityQuery eq)
                {
                    eq.All(clrType);
                }
            }

            var nativeResults = new HashSet<EntityId>(query.Build());
            results = scriptResults.Where(id => nativeResults.Contains(id)).ToList();
        }
        else if (scriptIndices.Length > 0)
        {
            results = _componentRegistry.ScriptStorage.QueryAll(scriptIndices);
        }
        else if (nativeIndices.Length > 0)
        {
            var query = _world.CreateQuery();
            foreach (var typeIdx in nativeIndices)
            {
                var clrType = _componentRegistry.GetClrType(typeIdx);
                if (clrType is not null && query is EntityQuery eq)
                {
                    eq.All(clrType);
                }
            }

            results = query.Build().ToList();
        }
        else
        {
            results = new List<EntityId>();
        }

        var arr = new GGArray(results.Count);

        for (var i = 0; i < results.Count; i++)
        {
            arr[i] = GGValue.FromEntity(results[i]);
        }

        _state.MemoryManager.Allocate(arr);
        _state.Push(GGValue.FromArray(arr));
    }

    private void ExecuteQueryAny()
    {
        var count = ReadInt32();
        var typeIndices = new int[count];

        for (var i = 0; i < count; i++)
        {
            typeIndices[i] = ReadInt32();
        }

        if (_world is null)
        {
            _state.Push(GGValue.Null);
            return;
        }

        var scriptIndices = typeIndices.Where(idx => _componentRegistry.IsScriptComponent(idx)).ToArray();
        var nativeIndices = typeIndices.Where(idx => !_componentRegistry.IsScriptComponent(idx)).ToArray();

        var resultSet = new HashSet<EntityId>();

        if (scriptIndices.Length > 0)
        {
            foreach (var id in _componentRegistry.ScriptStorage.QueryAny(scriptIndices))
            {
                resultSet.Add(id);
            }
        }

        if (nativeIndices.Length > 0)
        {
            var query = _world.CreateQuery();
            foreach (var typeIdx in nativeIndices)
            {
                var clrType = _componentRegistry.GetClrType(typeIdx);
                if (clrType is not null && query is EntityQuery eq)
                {
                    eq.Any(clrType);
                }
            }

            foreach (var id in query.Build())
            {
                resultSet.Add(id);
            }
        }

        var results = resultSet.ToList();
        var arr = new GGArray(results.Count);

        for (var i = 0; i < results.Count; i++)
        {
            arr[i] = GGValue.FromEntity(results[i]);
        }

        _state.MemoryManager.Allocate(arr);
        _state.Push(GGValue.FromArray(arr));
    }

    private void ExecuteQueryWith()
    {
        var typeIdx = ReadInt32();

        if (_world is null)
        {
            _state.Push(GGValue.Null);
            return;
        }

        List<EntityId> results;

        if (_componentRegistry.IsScriptComponent(typeIdx))
        {
            results = _componentRegistry.ScriptStorage.QueryWith(typeIdx);
        }
        else
        {
            var clrType = _componentRegistry.GetClrType(typeIdx);
            if (clrType is null)
            {
                _state.Push(GGValue.Null);
                return;
            }

            var query = _world.CreateQuery();
            if (query is EntityQuery eq)
            {
                eq.All(clrType);
            }

            results = query.Build().ToList();
        }

        var arr = new GGArray(results.Count);

        for (var i = 0; i < results.Count; i++)
        {
            arr[i] = GGValue.FromEntity(results[i]);
        }

        _state.MemoryManager.Allocate(arr);
        _state.Push(GGValue.FromArray(arr));
    }

    private void ExecuteQueryWithout()
    {
        var typeIdx = ReadInt32();

        if (_world is null)
        {
            _state.Push(GGValue.Null);
            return;
        }

        List<EntityId> results;

        if (_componentRegistry.IsScriptComponent(typeIdx))
        {
            if (_world is World concreteWorld)
            {
                var allEntities = new List<EntityId>();
                for (uint i = 1; i < concreteWorld.Entities.Capacity; i++)
                {
                    var id = new EntityId(i, 0);
                    if (concreteWorld.Entities.IsAlive(id))
                    {
                        allEntities.Add(id);
                    }
                }

                results = _componentRegistry.ScriptStorage.QueryWithout(typeIdx, allEntities);
            }
            else
            {
                results = new List<EntityId>();
            }
        }
        else
        {
            var clrType = _componentRegistry.GetClrType(typeIdx);
            if (clrType is null)
            {
                _state.Push(GGValue.Null);
                return;
            }

            var query = _world.CreateQuery();
            if (query is EntityQuery eq)
            {
                eq.None(clrType);
            }

            results = query.Build().ToList();
        }

        var arr = new GGArray(results.Count);

        for (var i = 0; i < results.Count; i++)
        {
            arr[i] = GGValue.FromEntity(results[i]);
        }

        _state.MemoryManager.Allocate(arr);
        _state.Push(GGValue.FromArray(arr));
    }

    private void ExecuteDefineComponent()
    {
        var nameIdx = ReadInt32();
        var fieldCount = ReadInt32();

        var name = ReadConstant()?.ToString() ?? $"component_{nameIdx}";
        var fieldNames = new string[fieldCount];

        for (var i = 0; i < fieldCount; i++)
        {
            fieldNames[i] = ReadConstant()?.ToString() ?? $"field_{i}";
        }

        var existingIdx = _componentRegistry.GetIndex(name);
        if (existingIdx >= 0)
        {
            _state.Push(GGValue.FromInt(existingIdx));
            return;
        }

        var scriptType = new ScriptComponentType(name, fieldNames);
        var typeIdx = _componentRegistry.RegisterScriptComponent(scriptType);

        var ggStruct = new GGStruct(name, fieldNames);
        _state.MemoryManager.Allocate(ggStruct);
        _state.Push(GGValue.FromInt(typeIdx));
    }

    private void ExecuteDefineSystem()
    {
        var nameIdx = ReadInt32();
        var phaseValue = ReadInt32();
        var funcAddr = ReadInt32();

        var name = ReadConstant()?.ToString() ?? $"system_{nameIdx}";
        var phase = (SystemPhase)phaseValue;

        var systemInfo = new ScriptSystemInfo(name, phase, funcAddr, _nextSystemIndex);
        _scriptSystemScheduler.RegisterSystem(systemInfo);

        _state.Push(GGValue.FromInt(_nextSystemIndex));
        _nextSystemIndex++;
    }

    private void ExecuteSystemSchedule()
    {
        var systemIdx = ReadInt32();
        var dependencyCount = ReadInt32();

        for (var i = 0; i < dependencyCount; i++)
        {
            var dependsOnIdx = ReadInt32();
            _scriptSystemScheduler.AddDependency(systemIdx, dependsOnIdx);
        }

        _state.Push(GGValue.Null);
    }

    private void ExecuteWorldUpdate()
    {
        var deltaRaw = ReadFloat32();

        if (_world is World concreteWorld)
        {
            concreteWorld.Update(deltaRaw);
        }

        var scriptSystems = _scriptSystemScheduler.GetExecutionOrder();
        foreach (var system in scriptSystems)
        {
            ExecuteScriptSystem(system, deltaRaw);
        }

        _state.Push(GGValue.Null);
    }

    /// <summary>
    /// 执行单个脚本系统：保存当前执行状态，跳转到系统函数地址执行，完成后恢复。
    /// 脚本系统函数签约为：function(delta: float)
    /// </summary>
    private void ExecuteScriptSystem(ScriptSystemInfo system, float delta)
    {
        if (system.FunctionAddress < 0 || _instructions is null || system.FunctionAddress >= _instructions.Length)
        {
            return;
        }

        var savedIP = _state.IP;
        var savedInstructions = _instructions;

        _state.Push(GGValue.FromFloat(delta));

        _state.StackInternal.PushFrame(_state.IP, _state.StackInternal.SP, 1);

        var frame = _state.StackInternal.CurrentFrame;
        if (frame.HasValue)
        {
            frame.Value.Locals[0] = GGValue.FromFloat(delta);
        }

        _state.IP = system.FunctionAddress;

        while (_running)
        {
            if (_state.IP < 0 || _state.IP >= _instructions.Length)
            {
                break;
            }

            var opCode = (OpCode)_instructions[_state.IP];
            if (opCode == OpCode.Return)
            {
                _state.IP++;
                var returnFrame = _state.StackInternal.PopFrame();
                _state.IP = returnFrame.ReturnAddress;
                break;
            }

            if (!Step())
            {
                break;
            }
        }

        _state.IP = savedIP;
        _instructions = savedInstructions;
    }

    private EntityId ResolveEntityId(GGValue value)
    {
        if (value.IsEntity)
        {
            return value.ToEntityId;
        }

        if (value.IsInt)
        {
            return new EntityId((uint)value.IntValue, 0);
        }

        throw new VMRuntimeException($"无法将 {value.Type} 转换为实体 ID");
    }

    #endregion

    #region 字符串操作

    private void ExecutePushString()
    {
        var idx = ReadInt32();

        if (_state.CurrentModule?.Constants.TryGetValue(idx.ToString(), out var value) == true)
        {
            var str = value?.ToString();
            _state.Push(str is not null ? GGValue.FromString(new GGString(str)) : GGValue.Null);
        }
        else
        {
            _state.Push(GGValue.Null);
        }
    }

    private void ExecuteConcatString()
    {
        var bVal = _state.Pop();
        var aVal = _state.Pop();
        var b = bVal.Type == GGValueType.String ? bVal.StringValue?.Value ?? "" : bVal.ToString() ?? "";
        var a = aVal.Type == GGValueType.String ? aVal.StringValue?.Value ?? "" : aVal.ToString() ?? "";
        _state.Push(GGValue.FromString(new GGString(string.Concat(a, b))));
    }

    private void ExecuteStringLength()
    {
        var val = _state.Pop();
        var str = val.Type == GGValueType.String ? val.StringValue?.Value ?? "" : val.ToString() ?? "";
        _state.Push(GGValue.FromInt(str.Length));
    }

    private void ExecuteStringGetChar()
    {
        var idx = (int)_state.Pop().IntValue;
        var val = _state.Pop();
        var str = val.Type == GGValueType.String ? val.StringValue?.Value ?? "" : val.ToString() ?? "";

        if (idx >= 0 && idx < str.Length)
        {
            _state.Push(GGValue.FromString(new GGString(str[idx].ToString())));
        }
        else
        {
            _state.Push(GGValue.Null);
        }
    }

    #endregion

    #region 数组操作

    private void ExecuteNewArray()
    {
        var size = ReadInt32();
        var arr = new GGArray(size);
        _state.MemoryManager.Allocate(arr);
        _state.Push(GGValue.FromArray(arr));
    }

    private void ExecuteArrayGet()
    {
        var index = (int)_state.Pop().IntValue;
        var target = _state.Pop();

        if (TryGetHeapObject<GGArray>(target, out var arr) && index >= 0 && index < arr.Count)
        {
            _state.Push(arr[index]);
            return;
        }

        _state.Push(GGValue.Null);
    }

    private void ExecuteArraySet()
    {
        var value = _state.Pop();
        var index = (int)_state.Pop().IntValue;
        var target = _state.Pop();

        if (TryGetHeapObject<GGArray>(target, out var arr) && index >= 0 && index < arr.Capacity)
        {
            arr[index] = value;
        }
    }

    private void ExecuteArrayLength()
    {
        var target = _state.Pop();

        if (TryGetHeapObject<GGArray>(target, out var arr))
        {
            _state.Push(GGValue.FromInt(arr.Count));
            return;
        }

        _state.Push(GGValue.FromInt(0));
    }

    #endregion

    #region 闭包操作

    private void ExecuteMakeClosure()
    {
        var funcAddr = ReadInt32();
        var closure = new GGClosure(funcAddr, 0);
        _state.MemoryManager.Allocate(closure);
        _state.Push(GGValue.FromClosure(closure));
    }

    private void ExecuteGetUpvalue()
    {
        var idx = (int)_state.Pop().IntValue;
        var target = _state.Pop();

        if (TryGetHeapObject<GGClosure>(target, out var closure))
        {
            _state.Push(closure.GetUpvalue(idx));
            return;
        }

        _state.Push(GGValue.Null);
    }

    private void ExecuteSetUpvalue()
    {
        var value = _state.Pop();
        var idx = (int)_state.Pop().IntValue;
        var target = _state.Pop();

        if (TryGetHeapObject<GGClosure>(target, out var closure))
        {
            closure.SetUpvalue(idx, value);
        }
    }

    #endregion

    #region 类型检查

    private void ExecuteIsNull()
    {
        var value = _state.Pop();
        _state.Push(GGValue.FromBool(value.IsNull));
    }

    private void ExecuteIsType()
    {
        var typeIdx = ReadInt32();
        var typeName = ReadConstant()?.ToString() ?? "";
        var value = _state.Pop();

        var result = typeName switch
        {
            "int" or "i32" or "i64" => value.IsInt,
            "float" or "f32" or "f64" => value.IsFloat,
            "bool" => value.IsBool,
            "string" => value.Type == GGValueType.String,
            "array" => value.Type == GGValueType.Array,
            "object" => value.Type == GGValueType.Object,
            "entity" => value.IsEntity,
            _ => false
        };

        _state.Push(GGValue.FromBool(result));
    }

    private void ExecuteTypeOf()
    {
        var typeIdx = ReadInt32();
        var value = _state.Pop();

        var typeName = value.Type switch
        {
            GGValueType.Null => "null",
            GGValueType.Int => "int",
            GGValueType.Float => "float",
            GGValueType.Bool => "bool",
            GGValueType.String => "string",
            GGValueType.Object => "object",
            GGValueType.Array => "array",
            GGValueType.Struct => "struct",
            GGValueType.Closure => "closure",
            GGValueType.Entity => "entity",
            _ => "unknown"
        };

        _state.Push(GGValue.FromInt(typeName.GetHashCode()));
    }

    #endregion

    #region 辅助方法

    private void RefreshGCRoots()
    {
        var mm = _state.MemoryManager;
        mm.ClearRoots();

        var rawStack = _state.StackInternal.GetRawStack();
        mm.SetRootsFromStack(rawStack, _state.StackInternal.SP);

        foreach (var frame in _state.StackInternal.FramesInternal)
        {
            mm.SetRootsFromLocals(frame.Locals);
        }

        for (var i = 0; i < _state.GlobalCount; i++)
        {
            var globalValue = _state.GetGlobal(i);
            if (globalValue.Reference is IGCObject gcObj)
            {
                mm.AddRoot(gcObj);
            }
            else if (globalValue.IsInt)
            {
                var obj = mm.GetObject((int)globalValue.IntValue);
                if (obj is not null)
                {
                    mm.AddRoot(obj);
                }
            }
        }
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetHeapObject<T>(GGValue value, out T? obj) where T : class, IGCObject
    {
        obj = null;

        if (value.IsReference && value.Reference is T direct)
        {
            obj = direct;
            return true;
        }

        if (value.IsInt)
        {
            obj = _state.MemoryManager.GetObject<T>((int)value.IntValue);
            return obj is not null;
        }

        return false;
    }

    #endregion
}
