namespace Gnosis.AI.Spawn;

/// <summary>
/// AI 生成参数
/// </summary>
public struct AISpawnParams
{
    /// <summary>
    /// AI 类型名称
    /// </summary>
    public string TypeName { get; init; }

    /// <summary>
    /// 生成位置
    /// </summary>
    public float[] Position { get; init; }

    /// <summary>
    /// 行为树路径
    /// </summary>
    public string? BehaviorTreePath { get; init; }

    /// <summary>
    /// 是否使用对象池
    /// </summary>
    public bool UsePool { get; init; }

    /// <summary>
    /// 生成延迟（秒）
    /// </summary>
    public float SpawnDelay { get; init; }
}
