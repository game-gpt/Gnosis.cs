using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Archetype;

/// <summary>
/// Archetype 接口，表示具有相同组件组合的实体集合
/// </summary>
public interface IArchetype
{
    /// <summary>
    /// 此 Archetype 包含的组件类型集合
    /// </summary>
    IReadOnlySet<Type> ComponentTypes { get; }

    /// <summary>
    /// 此 Archetype 中的实体数量
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// 检查是否包含指定类型的组件
    /// </summary>
    bool HasComponent<T>() where T : struct;

    /// <summary>
    /// 获取此 Archetype 中的所有实体 ID
    /// </summary>
    IEnumerable<EntityId> GetEntities();

    /// <summary>
    /// 获取指定实体的组件引用
    /// </summary>
    ref T GetComponent<T>(EntityId entityId) where T : struct;

    /// <summary>
    /// 设置指定实体的组件
    /// </summary>
    void SetComponent<T>(EntityId entityId, T component) where T : struct;
}
