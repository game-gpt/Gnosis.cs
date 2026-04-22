using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Query;

namespace Gnosis.ECS.World;

/// <summary>
/// 世界接口，ECS 的顶层容器
/// </summary>
public interface IWorld
{
    /// <summary>
    /// 创建新实体
    /// </summary>
    EntityId CreateEntity();

    /// <summary>
    /// 销毁指定实体
    /// </summary>
    void DestroyEntity(EntityId entityId);

    /// <summary>
    /// 添加组件到指定实体
    /// </summary>
    void AddComponent<T>(EntityId entityId, T component) where T : struct;

    /// <summary>
    /// 获取指定实体的组件
    /// </summary>
    T GetComponent<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 设置指定实体的组件值，并自动标记变更
    /// </summary>
    void SetComponent<T>(EntityId entityId, T component) where T : struct;

    /// <summary>
    /// 移除指定实体的组件
    /// </summary>
    void RemoveComponent<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    bool HasComponent<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 创建查询构建器
    /// </summary>
    IQuery CreateQuery();

    /// <summary>
    /// 基于查询描述符创建查询构建器
    /// </summary>
    IQuery CreateQuery(QueryDescription description);

    /// <summary>
    /// 获取匹配指定组件组合的 Archetype
    /// </summary>
    IArchetype GetArchetype(params Type[] componentTypes);

    /// <summary>
    /// 活跃实体数量
    /// </summary>
    int EntityCount { get; }
}
