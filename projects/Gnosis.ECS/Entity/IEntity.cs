namespace Gnosis.ECS.Entity;

/// <summary>
/// 实体接口，表示 ECS 世界中的一个实体
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 实体标识符
    /// </summary>
    EntityId Id { get; }
}
