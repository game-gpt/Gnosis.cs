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
    private readonly IGameWorld _world;

    /// <summary>
    ///     GameBuiltin ID → 运行时分派处理器的映射表
    ///     桥接编译期降级规则（EcsLoweringRules）与运行时 IGameWorld 方法
    /// </summary>
    private readonly Dictionary<long, Func<Value[], Value>> _builtinDispatchTable;

    /// <summary>
    ///     查询规格注册表：specHash → (all, any, none)
    ///     由于降级规则只传递 specHash，需要在编译期注册查询规格
    /// </summary>
    private readonly Dictionary<int, QuerySpecRecord> _querySpecRegistry;

    public GnosisGameVM(IGameWorld world)
    {
        _inner = new NyarVM();
        _world = world;
        _builtinDispatchTable = BuildBuiltinDispatchTable();
        _querySpecRegistry = new Dictionary<int, QuerySpecRecord>();

        foreach (var (builtinId, handler) in _builtinDispatchTable)
        {
            _inner.RegisterBuiltinDispatcher(builtinId, handler);
        }
    }

    /// <summary>
    ///     加载模块
    /// </summary>
    public void LoadModule(NyarModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module.RawBytecode is null)
        {
            var encoded = NyarModuleConverter.Encode(module);
            module.RawBytecode = encoded;
        }

        _inner.Load(module);
    }

    /// <summary>
    ///     加载字节码
    /// </summary>
    public void LoadBytecode(byte[] bytecode)
    {
        _inner.Load(bytecode);
    }

    /// <summary>
    ///     执行函数
    /// </summary>
    public Value Run(string moduleName, string functionName, params Value[] args)
    {
        return _inner.Run(moduleName, functionName, args);
    }

    /// <summary>
    ///     执行一帧的世界更新
    /// </summary>
    public void Tick(double deltaTime)
    {
        _world.Update(deltaTime);
    }

    /// <summary>
    ///     获取游戏世界
    /// </summary>
    public IGameWorld World => _world;

    /// <summary>
    ///     注册查询规格，供 DispatchEcsQuery 在运行时查找
    ///     应在编译期/加载期调用，将降级规则生成的 specHash 与实际查询参数关联
    /// </summary>
    /// <param name="specHash">查询规格哈希值</param>
    /// <param name="all">必须拥有的组件类型名列表</param>
    /// <param name="any">至少拥有一个的组件类型名列表</param>
    /// <param name="none">不能拥有的组件类型名列表</param>
    public void RegisterQuerySpec(int specHash, string[] all, string[] any, string[] none)
    {
        _querySpecRegistry[specHash] = new QuerySpecRecord(all, any, none);
    }

    /// <summary>
    ///     根据 GameBuiltin ID 分派到 IGameWorld 对应方法
    /// </summary>
    public Value DispatchBuiltin(long builtinId, Value[] args)
    {
        if (_builtinDispatchTable.TryGetValue(builtinId, out var handler))
        {
            return handler(args);
        }

        return Value.Null;
    }

    #region Builtin 分派表构建

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
        return ConvertToValue(value);
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
        if (args.Length < 1) return Value.FromObject(new List<Value>());

        var specHash = (int)args[0].Double;

        if (!_querySpecRegistry.TryGetValue(specHash, out var spec))
        {
            return Value.FromObject(new List<Value>());
        }

        var entityIds = _world.QueryEntities(spec.All, spec.Any, spec.None);

        var resultValues = new List<Value>(entityIds.Count);
        foreach (var id in entityIds)
        {
            resultValues.Add(Value.FromDouble(id));
        }

        return Value.FromObject(resultValues);
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

    /// <summary>
    ///     将 C# 对象转换为 Nyar Value
    /// </summary>
    private static Value ConvertToValue(object? value)
    {
        return value switch
        {
            int i => Value.FromDouble(i),
            long l => Value.FromDouble(l),
            float f => Value.FromDouble(f),
            double d => Value.FromDouble(d),
            bool b => Value.FromDouble(b ? 1 : 0),
            string s => Value.FromObject(s),
            _ => Value.Null
        };
    }

    #endregion

    #region 查询规格记录

    /// <summary>
    ///     查询规格记录，存储 all/any/none 组件类型名列表
    /// </summary>
    private sealed record QuerySpecRecord
    {
        public string[] All { get; }
        public string[] Any { get; }
        public string[] None { get; }

        public QuerySpecRecord(string[] all, string[] any, string[] none)
        {
            All = all;
            Any = any;
            None = none;
        }
    }

    #endregion
}
