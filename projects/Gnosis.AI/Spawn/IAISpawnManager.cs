namespace Gnosis.AI.Spawn;

/// <summary>
/// AI 生成管理器接口，管理 AI 实体的池化与生成
/// </summary>
public interface IAISpawnManager
{
    /// <summary>
    /// 当前存活 AI 数量
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// 池中可用 AI 数量
    /// </summary>
    int PooledCount { get; }

    /// <summary>
    /// 最大 AI 数量
    /// </summary>
    int MaxCount { get; }

    /// <summary>
    /// 生成 AI 实体
    /// </summary>
    string Spawn(AISpawnParams parameters);

    /// <summary>
    /// 回收 AI 实体到池中
    /// </summary>
    void Despawn(string entityId);

    /// <summary>
    /// 预热对象池
    /// </summary>
    void Preload(AISpawnParams parameters, int count);

    /// <summary>
    /// 清空对象池
    /// </summary>
    void ClearPool();

    /// <summary>
    /// 更新生成管理器
    /// </summary>
    void Update(float delta);
}
