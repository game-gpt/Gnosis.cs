using Nyar.Core;
using Nyar.Core.Types;
using Nyar.Core.VM;
using Nyar.Core.VM.Bytecode;
using Nyar.Core.VM.Runtime;
using Nyar.Dialect.Game.Rules;

using NyarValueType = Nyar.Core.ValueType;

namespace Gnosis.VM;

/// <summary>
///     Gnosis 游戏 VM，基于 Game 方言的 ECS 优化虚拟机
///     与 NyarStandardVM 同级，复用 Nyar 元虚拟机框架，但针对游戏场景优化
///     核心职责：将 GameBuiltin ID (0x8001-0x800B) 运行时分派到 IGameWorld 方法
/// </summary>
public sealed class GnosisGameVM
{
    private readonly NyarVM _inner;
    private readonly BytecodeEncoder _encoder;
    private readonly IGameWorld _world;

    /// <summary>
    ///     GameBuiltin ID → 运行时分派处理器的映射表
    ///     桥接编译期降级规则（EcsLoweringRules）与运行时 IGameWorld 方法
    /// </summary>
    private readonly Dictionary<long, Func<Value[], Value>> _builtinDispatchTable;

    /// <summary>
    ///     初始化 GnosisGameVM
    /// </summary>
    /// <param name="world">游戏世界实例</param>
    public GnosisGameVM(IGameWorld world)
    {
        _inner = new NyarVM();
        _encoder = new BytecodeEncoder();
        _world = world;
        _builtinDispatchTable = BuildBuiltinDispatchTable();

        foreach (var (builtinId, handler) in _builtinDispatchTable)
        {
            _inner.RegisterBuiltinDispatcher(builtinId, handler);
        }
    }

    /// <summary>
    ///     加载模块
    /// </summary>
    /// <param name="module">要加载的模块</param>
    public void LoadModule(NyarModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module.RawBytecode is null)
        {
            var encoded = _encoder.Encode(module);
            module.RawBytecode = encoded;
        }

        _inner.Load(module);
    }

    /// <summary>
    ///     加载字节码
    /// </summary>
    /// <param name="bytecode">字节码数据</param>
    public void LoadBytecode(byte[] bytecode)
    {
        _inner.Load(bytecode);
    }

    /// <summary>
    ///     执行函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="functionName">函数名称</param>
    /// <param name="args">函数参数</param>
    /// <returns>函数返回值</returns>
    public Value Run(string moduleName, string functionName, params Value[] args)
    {
        return _inner.Run(moduleName, functionName, args);
    }

    /// <summary>
    ///     执行一帧的世界更新
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（毫秒）</param>
    public void Tick(double deltaTime)
    {
        _world.Update(deltaTime);
    }

    /// <summary>
    ///     获取游戏世界
    /// </summary>
    public IGameWorld World => _world;

    /// <summary>
    ///     根据 GameBuiltin ID 分派到 IGameWorld 对应方法
    ///     这是编译期降级（EcsLoweringRules）与运行时执行的桥接点
    /// </summary>
    /// <param name="builtinId">GameBuiltin 内置函数 ID</param>
    /// <param name="args">运行时参数</param>
    /// <returns>执行结果</returns>
    public Value DispatchBuiltin(long builtinId, Value[] args)
    {
        if (_builtinDispatchTable.TryGetValue(builtinId, out var handler))
        {
            return handler(args);
        }

        return Value.Null;
    }

    #region Builtin 分派表构建

    /// <summary>
    ///     构建 GameBuiltin ID → IGameWorld 方法的运行时分派表
    ///     每个分派器将 Nyar Value 参数转换为 IGameWorld 接口所需的类型
    /// </summary>
    private Dictionary<long, Func<Value[], Value>> BuildBuiltinDispatchTable()
    {
        return new Dictionary<long, Func<Value[], Value>>
        {
            [(long)GameBuiltin.EcsSpawn] = DispatchEcsSpawn,
            [(long)GameBuiltin.EcsDestroy] = DispatchEcsDestroy,
            [(long)GameBuiltin.EcsAddComponent] = DispatchEcsAddComponent,
            [(long)GameBuiltin.EcsGetComponent] = DispatchEcsGetComponent,
            [(long)GameBuiltin.EcsSetComponent] = DispatchEcsSetComponent,
            [(long)GameBuiltin.EcsRemoveComponent] = DispatchEcsRemoveComponent,
            [(long)GameBuiltin.EcsHasComponent] = DispatchEcsHasComponent,
            [(long)GameBuiltin.EcsQuery] = DispatchEcsQuery,
            [(long)GameBuiltin.EcsWorldUpdate] = DispatchEcsWorldUpdate
        };
    }

    private Value DispatchEcsSpawn(Value[] args)
    {
        var entityId = _world.SpawnEntity();
        return Value.FromDouble(entityId);
    }

    private Value DispatchEcsDestroy(Value[] args)
    {
        if (args.Length < 1) return Value.Null;

        var entityId = (long)args[0].Double;
        _world.DestroyEntity(entityId);
        return Value.Null;
    }

    private Value DispatchEcsAddComponent(Value[] args)
    {
        if (args.Length < 2) return Value.Null;

        var entityId = (long)args[0].Double;
        var componentType = ExtractString(args[1]);
        var fields = new Dictionary<string, object?>();

        _world.AddComponent(entityId, componentType, fields);
        return Value.Null;
    }

    private Value DispatchEcsGetComponent(Value[] args)
    {
        if (args.Length < 3) return Value.Null;

        var entityId = (long)args[0].Double;
        var componentType = ExtractString(args[1]);
        var fieldName = ExtractString(args[2]);

        var value = _world.GetComponent(entityId, componentType, fieldName);
        return value switch
        {
            int i => Value.FromDouble(i),
            long l => Value.FromDouble(l),
            float f => Value.FromDouble(f),
            double d => Value.FromDouble(d),
            bool b => Value.FromDouble(b ? 1 : 0),
            string s => Value.FromDouble(s.GetHashCode()),
            _ => Value.Null
        };
    }

    private Value DispatchEcsSetComponent(Value[] args)
    {
        if (args.Length < 4) return Value.Null;

        var entityId = (long)args[0].Double;
        var componentType = ExtractString(args[1]);
        var fieldName = ExtractString(args[2]);
        object? value = args[3].Type == NyarValueType.Double
            ? args[3].Double
            : args[3].Type == NyarValueType.Int
                ? args[3].Int
                : ExtractString(args[3]);

        _world.SetComponent(entityId, componentType, fieldName, value);
        return Value.Null;
    }

    private Value DispatchEcsRemoveComponent(Value[] args)
    {
        if (args.Length < 2) return Value.Null;

        var entityId = (long)args[0].Double;
        var componentType = ExtractString(args[1]);

        _world.RemoveComponent(entityId, componentType);
        return Value.Null;
    }

    private Value DispatchEcsHasComponent(Value[] args)
    {
        if (args.Length < 2) return Value.FromDouble(0);

        var entityId = (long)args[0].Double;
        var componentType = ExtractString(args[1]);

        var has = _world.HasComponent(entityId, componentType);
        return Value.FromDouble(has ? 1 : 0);
    }

    private Value DispatchEcsQuery(Value[] args)
    {
        return Value.Null;
    }

    private Value DispatchEcsWorldUpdate(Value[] args)
    {
        var deltaTime = args.Length > 0 ? args[0].Double : 0.0;
        _world.Update(deltaTime);
        return Value.Null;
    }

    /// <summary>
    ///     从 Value 中提取字符串值
    /// </summary>
    private static string ExtractString(Value value)
    {
        if (value.String is string s) return s;
        if (value.Type == NyarValueType.Int) return value.Int.ToString();
        if (value.Type == NyarValueType.Double) return value.Double.ToString();
        return value.ToString() ?? "";
    }

    #endregion
}
