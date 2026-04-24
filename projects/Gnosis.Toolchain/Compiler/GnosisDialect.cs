using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;
using Nyar.Dialect.Game.Rules;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 方言节点描述符，描述每个领域节点的签名、操作码映射和成本权重
/// </summary>
public sealed record GnosisNodeDescriptor(
    string Name,
    IrOpcode IrOpcode,
    OpCode BytecodeOpCode,
    string Category,
    string[] ParameterTypes,
    string ReturnType,
    double CostWeight);

/// <summary>
///     Gnosis 方言定义，注册 ECS/AI/导航/渲染/网络/物理/内存/协程等游戏领域节点和规则
///     提供 GameBuiltin ID (0x8001-0x800B) → Gnosis OpCode (0x80-0x8E) 的映射桥接
/// </summary>
public static class GnosisDialect
{
    /// <summary>
    ///     Gnosis 方言 ID
    /// </summary>
    public const int DialectId = 100;

    /// <summary>
    ///     Gnosis 方言名称
    /// </summary>
    public const string DialectName = "gnosis";

    /// <summary>
    ///     已注册的节点描述符表（节点名称 → 描述符）
    /// </summary>
    private static readonly Dictionary<string, GnosisNodeDescriptor> s_nodeDescriptors = new();

    /// <summary>
    ///     获取所有已注册的节点描述符
    /// </summary>
    public static IReadOnlyDictionary<string, GnosisNodeDescriptor> NodeDescriptors => s_nodeDescriptors;

    /// <summary>
    ///     注册 Gnosis 方言到 Nyar 编译器
    ///     将 Gnosis 特有的降级规则和优化规则添加到编译管线
    /// </summary>
    public static void Register()
    {
        RegisterEcsNodes();
        RegisterAiNodes();
        RegisterNavigationNodes();
        RegisterRenderingNodes();
        RegisterNetworkNodes();
        RegisterPhysicsNodes();
        RegisterMemoryNodes();
        RegisterCoroutineNodes();
    }

    #region GameBuiltin → OpCode 映射

    /// <summary>
    ///     GameBuiltin ID (0x8001-0x800B) → Gnosis VM OpCode (0x80-0x8E) 映射表
    ///     桥接 Nyar IR 层的内置函数标识与 Gnosis 字节码层的操作码
    /// </summary>
    public static readonly Dictionary<long, OpCode> BuiltinToOpCodeMap = new()
    {
        [(long)GameBuiltin.EcsSpawn] = OpCode.SpawnEntity,
        [(long)GameBuiltin.EcsDestroy] = OpCode.DestroyEntity,
        [(long)GameBuiltin.EcsAddComponent] = OpCode.AddComponent,
        [(long)GameBuiltin.EcsGetComponent] = OpCode.GetComponent,
        [(long)GameBuiltin.EcsSetComponent] = OpCode.SetComponent,
        [(long)GameBuiltin.EcsRemoveComponent] = OpCode.RemoveComponent,
        [(long)GameBuiltin.EcsHasComponent] = OpCode.HasComponent,
        [(long)GameBuiltin.EcsDefineComponent] = OpCode.DefineComponent,
        [(long)GameBuiltin.EcsDefineSystem] = OpCode.DefineSystem,
        [(long)GameBuiltin.EcsQuery] = OpCode.QueryAll,
        [(long)GameBuiltin.EcsWorldUpdate] = OpCode.WorldUpdate
    };

    /// <summary>
    ///     将 GameBuiltin ID 转换为 Gnosis VM OpCode
    /// </summary>
    /// <param name="builtinId">GameBuiltin ID</param>
    /// <returns>对应的 OpCode，未找到返回 null</returns>
    public static OpCode? TryMapBuiltinToOpCode(long builtinId)
    {
        return BuiltinToOpCodeMap.GetValueOrDefault(builtinId);
    }

    #endregion

    #region 节点查询

    /// <summary>
    ///     根据节点名称查找描述符
    /// </summary>
    public static GnosisNodeDescriptor? FindNodeDescriptor(string name)
    {
        return s_nodeDescriptors.GetValueOrDefault(name);
    }

    /// <summary>
    ///     按类别查找节点描述符
    /// </summary>
    public static IReadOnlyList<GnosisNodeDescriptor> FindNodeDescriptorsByCategory(string category)
    {
        return s_nodeDescriptors.Values.Where(d => d.Category == category).ToList();
    }

    #endregion

    #region ECS 节点

    private static void RegisterEcsNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "SpawnEntity", IrOpcode.SpawnEntity, OpCode.SpawnEntity,
            "ecs", ["string?"], "i64", GnosisCostHook.EcsCosts.EntityCreate));

        RegisterNode(new GnosisNodeDescriptor(
            "DestroyEntity", IrOpcode.DestroyEntity, OpCode.DestroyEntity,
            "ecs", ["i64"], "void", GnosisCostHook.EcsCosts.EntityDestroy));

        RegisterNode(new GnosisNodeDescriptor(
            "AddComponent", IrOpcode.AddComponent, OpCode.AddComponent,
            "ecs", ["i64", "string"], "void", GnosisCostHook.EcsCosts.ComponentAdd));

        RegisterNode(new GnosisNodeDescriptor(
            "GetComponent", IrOpcode.GetComponent, OpCode.GetComponent,
            "ecs", ["i64", "string", "string"], "any", GnosisCostHook.EcsCosts.ComponentGet));

        RegisterNode(new GnosisNodeDescriptor(
            "SetComponent", IrOpcode.SetComponent, OpCode.SetComponent,
            "ecs", ["i64", "string", "string", "any"], "void", GnosisCostHook.EcsCosts.ComponentSet));

        RegisterNode(new GnosisNodeDescriptor(
            "RemoveComponent", IrOpcode.RemoveComponent, OpCode.RemoveComponent,
            "ecs", ["i64", "string"], "void", GnosisCostHook.EcsCosts.ComponentRemove));

        RegisterNode(new GnosisNodeDescriptor(
            "HasComponent", IrOpcode.HasComponent, OpCode.HasComponent,
            "ecs", ["i64", "string"], "bool", GnosisCostHook.EcsCosts.ComponentGet));

        RegisterNode(new GnosisNodeDescriptor(
            "DefineComponent", IrOpcode.DefineComponent, OpCode.DefineComponent,
            "ecs", ["string"], "void", GnosisCostHook.EcsCosts.ComponentAdd));

        RegisterNode(new GnosisNodeDescriptor(
            "DefineSystem", IrOpcode.DefineSystem, OpCode.DefineSystem,
            "ecs", ["string", "string", "function"], "void", GnosisCostHook.EcsCosts.ComponentAdd));

        RegisterNode(new GnosisNodeDescriptor(
            "QueryAll", IrOpcode.QueryAll, OpCode.QueryAll,
            "ecs", ["string"], "query", GnosisCostHook.EcsCosts.QueryExecute));

        RegisterNode(new GnosisNodeDescriptor(
            "QueryAny", IrOpcode.QueryAny, OpCode.QueryAny,
            "ecs", ["string"], "query", GnosisCostHook.EcsCosts.QueryExecute));

        RegisterNode(new GnosisNodeDescriptor(
            "QueryWith", IrOpcode.QueryWith, OpCode.QueryWith,
            "ecs", ["query", "string"], "query", GnosisCostHook.EcsCosts.QueryIterate));

        RegisterNode(new GnosisNodeDescriptor(
            "QueryWithout", IrOpcode.QueryWithout, OpCode.QueryWithout,
            "ecs", ["query", "string"], "query", GnosisCostHook.EcsCosts.QueryIterate));

        RegisterNode(new GnosisNodeDescriptor(
            "SystemSchedule", IrOpcode.SystemSchedule, OpCode.SystemSchedule,
            "ecs", ["system", "system[]"], "void", GnosisCostHook.EcsCosts.ComponentSet));

        RegisterNode(new GnosisNodeDescriptor(
            "WorldUpdate", IrOpcode.WorldUpdate, OpCode.WorldUpdate,
            "ecs", ["f64"], "void", GnosisCostHook.EcsCosts.EntityCreate));
    }

    #endregion

    #region AI 节点

    private static void RegisterAiNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "BehaviorTreeTick", IrOpcode.CallNative, OpCode.CallNative,
            "ai", ["i64"], "i32", GnosisCostHook.AiCosts.BehaviorTreeTick));

        RegisterNode(new GnosisNodeDescriptor(
            "Pathfinding", IrOpcode.CallNative, OpCode.CallNative,
            "ai", ["vec3", "vec3"], "path", GnosisCostHook.AiCosts.Pathfinding));

        RegisterNode(new GnosisNodeDescriptor(
            "DecisionMaking", IrOpcode.CallNative, OpCode.CallNative,
            "ai", ["i64", "context"], "i32", GnosisCostHook.AiCosts.DecisionMaking));
    }

    #endregion

    #region 导航节点

    private static void RegisterNavigationNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "NavMeshQuery", IrOpcode.CallNative, OpCode.CallNative,
            "navigation", ["vec3", "vec3"], "path", GnosisCostHook.AiCosts.Pathfinding));

        RegisterNode(new GnosisNodeDescriptor(
            "NavMeshRaycast", IrOpcode.CallNative, OpCode.CallNative,
            "navigation", ["vec3", "vec3"], "hit", GnosisCostHook.PhysicsCosts.Raycast));
    }

    #endregion

    #region 渲染节点

    private static void RegisterRenderingNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "DrawCall", IrOpcode.CallNative, OpCode.CallNative,
            "rendering", ["mesh", "material", "transform"], "void", GnosisCostHook.RenderingCosts.DrawCall));

        RegisterNode(new GnosisNodeDescriptor(
            "ShaderSwitch", IrOpcode.CallNative, OpCode.CallNative,
            "rendering", ["shader"], "void", GnosisCostHook.RenderingCosts.ShaderSwitch));

        RegisterNode(new GnosisNodeDescriptor(
            "TextureBind", IrOpcode.CallNative, OpCode.CallNative,
            "rendering", ["texture", "i32"], "void", GnosisCostHook.RenderingCosts.TextureBind));
    }

    #endregion

    #region 网络节点

    private static void RegisterNetworkNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "RpcCall", IrOpcode.CallNative, OpCode.CallNative,
            "network", ["string", "any[]"], "any", GnosisCostHook.NetworkCosts.RpcCall));

        RegisterNode(new GnosisNodeDescriptor(
            "StateSync", IrOpcode.CallNative, OpCode.CallNative,
            "network", ["i64", "string", "any"], "void", GnosisCostHook.NetworkCosts.StateSync));

        RegisterNode(new GnosisNodeDescriptor(
            "Broadcast", IrOpcode.CallNative, OpCode.CallNative,
            "network", ["string", "any"], "void", GnosisCostHook.NetworkCosts.Broadcast));
    }

    #endregion

    #region 物理节点

    private static void RegisterPhysicsNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "Raycast", IrOpcode.CallNative, OpCode.CallNative,
            "physics", ["vec3", "vec3", "f64"], "hit", GnosisCostHook.PhysicsCosts.Raycast));

        RegisterNode(new GnosisNodeDescriptor(
            "CollisionCheck", IrOpcode.CallNative, OpCode.CallNative,
            "physics", ["i64", "i64"], "bool", GnosisCostHook.PhysicsCosts.CollisionCheck));

        RegisterNode(new GnosisNodeDescriptor(
            "RigidBodyUpdate", IrOpcode.CallNative, OpCode.CallNative,
            "physics", ["i64", "vec3", "quat"], "void", GnosisCostHook.PhysicsCosts.RigidBodyUpdate));
    }

    #endregion

    #region 内存管理节点

    private static void RegisterMemoryNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "PoolAllocate", IrOpcode.CallNative, OpCode.CallNative,
            "memory", ["string", "i32"], "ptr", GnosisCostHook.MemoryCosts.Allocation));

        RegisterNode(new GnosisNodeDescriptor(
            "PoolDeallocate", IrOpcode.CallNative, OpCode.CallNative,
            "memory", ["string", "ptr"], "void", GnosisCostHook.MemoryCosts.Deallocation));

        RegisterNode(new GnosisNodeDescriptor(
            "MemoryCopy", IrOpcode.CallNative, OpCode.CallNative,
            "memory", ["ptr", "ptr", "i32"], "void", GnosisCostHook.MemoryCosts.Copy));
    }

    #endregion

    #region 协程节点

    private static void RegisterCoroutineNodes()
    {
        RegisterNode(new GnosisNodeDescriptor(
            "CoroutineYield", IrOpcode.Yield, OpCode.Yield,
            "coroutine", [], "void", 1.0));

        RegisterNode(new GnosisNodeDescriptor(
            "CoroutineResume", IrOpcode.Resume, OpCode.Resume,
            "coroutine", ["handle"], "any", 1.0));

        RegisterNode(new GnosisNodeDescriptor(
            "CoroutineSpawn", IrOpcode.CallNative, OpCode.CallNative,
            "coroutine", ["function", "any[]"], "handle", 2.0));
    }

    #endregion

    #region 内部辅助

    /// <summary>
    ///     注册节点描述符到全局表
    /// </summary>
    private static void RegisterNode(GnosisNodeDescriptor descriptor)
    {
        s_nodeDescriptors[descriptor.Name] = descriptor;
    }

    #endregion
}
