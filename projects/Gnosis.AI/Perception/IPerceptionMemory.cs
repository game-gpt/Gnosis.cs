using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

/// <summary>
/// 感知记忆接口，管理 AI 对已感知目标的记忆
/// </summary>
public interface IPerceptionMemory
{
    /// <summary>
    /// 记忆条目列表
    /// </summary>
    IReadOnlyList<PerceptionMemoryEntry> Entries { get; }

    /// <summary>
    /// 记忆最大容量
    /// </summary>
    int MaxCapacity { get; }

    /// <summary>
    /// 记忆衰减时间（秒）
    /// </summary>
    float DecayTime { get; set; }

    /// <summary>
    /// 添加或更新记忆条目
    /// </summary>
    void AddOrUpdate(IAIStimulusSource target, Vector3 position);

    /// <summary>
    /// 移除记忆条目
    /// </summary>
    void Remove(IAIStimulusSource target);

    /// <summary>
    /// 查询是否记忆中包含指定目标
    /// </summary>
    bool Contains(IAIStimulusSource target);

    /// <summary>
    /// 获取指定目标的最后已知位置
    /// </summary>
    PerceptionMemoryEntry? GetEntry(IAIStimulusSource target);

    /// <summary>
    /// 更新记忆（衰减过期条目）
    /// </summary>
    void Update(float delta);

    /// <summary>
    /// 清空记忆
    /// </summary>
    void Clear();
}
