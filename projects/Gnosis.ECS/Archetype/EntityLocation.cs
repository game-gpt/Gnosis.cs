namespace Gnosis.ECS.Archetype;

/// <summary>
/// 实体在 Archetype 中的位置信息
/// </summary>
public readonly record struct EntityLocation(int ChunkIndex, int IndexInChunk);