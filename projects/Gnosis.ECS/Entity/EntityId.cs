namespace Gnosis.ECS.Entity;

/// <summary>
/// 实体标识符，包含 32 位索引和 32 位代际，总计 64 位。
/// 代际用于检测已销毁实体的旧引用，防止访问错误实体。
/// </summary>
public readonly record struct EntityId(uint Index, uint Generation)
{
    /// <summary>
    /// 空实体 ID，索引和代际均为 0
    /// </summary>
    public static readonly EntityId Null = new(0, 0);

    /// <summary>
    /// 是否为空实体 ID
    /// </summary>
    public bool IsNull => Index == 0 && Generation == 0;

    public override string ToString()
    {
        return $"Entity({Index}:{Generation})";
    }
}
