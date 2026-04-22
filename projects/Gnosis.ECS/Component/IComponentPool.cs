using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Component;

/// <summary>
/// 组件池接口，定义组件存储的基本操作
/// </summary>
public interface IComponentPool
{
    /// <summary>
    /// 组件类型
    /// </summary>
    Type ComponentType { get; }

    /// <summary>
    /// 组件数量
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 添加组件到指定实体
    /// </summary>
    void Add<T>(EntityId entityId, T component) where T : struct;

    /// <summary>
    /// 获取指定实体的组件
    /// </summary>
    T Get<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 移除指定实体的组件
    /// </summary>
    void Remove<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    bool Has<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 检查指定实体是否在此池中（非泛型版本）
    /// </summary>
    bool HasEntity(EntityId entityId);

    /// <summary>
    /// 移除指定实体的组件（非泛型版本）
    /// </summary>
    void RemoveEntity(EntityId entityId);

    /// <summary>
    /// 获取所有拥有此组件的实体 ID
    /// </summary>
    IReadOnlyList<EntityId> GetAllEntityIds();

    /// <summary>
    /// 获取指定实体的组件数据（非泛型，返回 object）
    /// </summary>
    object? GetComponentData(EntityId entityId);

    /// <summary>
    /// 为指定实体添加组件数据（非泛型）
    /// </summary>
    void AddComponentData(EntityId entityId, object component);
}
