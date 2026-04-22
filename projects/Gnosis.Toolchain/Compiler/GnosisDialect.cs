using Gnosis.IR.Graph;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 方言定义，注册 ECS/AI/导航/渲染/网络/物理/内存/协程等游戏领域节点和规则
/// </summary>
public static class GnosisDialect
{
    /// <summary>
    ///     注册 Gnosis 方言到 Nyar 编译器
    /// </summary>
    public static void Register()
    {
        // ECS 节点
        RegisterEcsNodes();

        // AI 节点
        RegisterAiNodes();

        // 导航节点
        RegisterNavigationNodes();

        // 渲染节点
        RegisterRenderingNodes();

        // 网络节点
        RegisterNetworkNodes();

        // 物理节点
        RegisterPhysicsNodes();

        // 内存管理节点
        RegisterMemoryNodes();

        // 协程节点
        RegisterCoroutineNodes();
    }

    #region ECS 节点

    private static void RegisterEcsNodes()
    {
        // EntityCreate, EntityDestroy, ComponentAdd, ComponentGet, ComponentSet,
        // ComponentRemove, ComponentHas, QueryExecute, QueryIterate, QueryCount,
        // EntityCreateBatch, EntityDestroyBatch 等节点注册
        // 实际注册逻辑由 Nyar 编译器插件系统处理
    }

    #endregion

    #region AI 节点

    private static void RegisterAiNodes()
    {
        // AIBehavior, AIDecision, AIStateMachine, AIGoal 等节点注册
    }

    #endregion

    #region 导航节点

    private static void RegisterNavigationNodes()
    {
        // NavMesh, Pathfinding, Waypoint 等节点注册
    }

    #endregion

    #region 渲染节点

    private static void RegisterRenderingNodes()
    {
        // RenderPass, Shader, Material, Mesh 等节点注册
    }

    #endregion

    #region 网络节点

    private static void RegisterNetworkNodes()
    {
        // NetworkSync, RpcCall, StateReplication 等节点注册
    }

    #endregion

    #region 物理节点

    private static void RegisterPhysicsNodes()
    {
        // RigidBody, Collider, Joint, Raycast 等节点注册
    }

    #endregion

    #region 内存管理节点

    private static void RegisterMemoryNodes()
    {
        // MemoryPool, ObjectPool, Arena 等节点注册
    }

    #endregion

    #region 协程节点

    private static void RegisterCoroutineNodes()
    {
        // Coroutine, Yield, Await 等节点注册
    }

    #endregion
}
