namespace Gnosis.AI.Perception;

/// <summary>
/// 感知记忆实现，管理 AI 对已感知目标的记忆，支持衰减过期
/// </summary>
public sealed class PerceptionMemory : IPerceptionMemory
{
    #region 字段

    private readonly Dictionary<IAIStimulusSource, PerceptionMemoryEntry> _entries = new();
    private float _elapsedTime;

    #endregion

    #region 属性

    /// <summary>
    /// 记忆条目列表
    /// </summary>
    public IReadOnlyList<PerceptionMemoryEntry> Entries => _entries.Values.ToList().AsReadOnly();

    /// <summary>
    /// 记忆最大容量
    /// </summary>
    public int MaxCapacity { get; }

    /// <summary>
    /// 记忆衰减时间（秒）
    /// </summary>
    public float DecayTime { get; set; }

    /// <summary>
    /// 当前记忆条目数量
    /// </summary>
    public int Count => _entries.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建感知记忆
    /// </summary>
    /// <param name="maxCapacity">最大容量</param>
    /// <param name="decayTime">衰减时间（秒）</param>
    public PerceptionMemory(int maxCapacity = 16, float decayTime = 5.0f)
    {
        MaxCapacity = maxCapacity;
        DecayTime = decayTime;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加或更新记忆条目
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <param name="position">目标位置</param>
    public void AddOrUpdate(IAIStimulusSource target, float[] position)
    {
        if (_entries.TryGetValue(target, out var existing))
        {
            var updated = new PerceptionMemoryEntry
            {
                Target = target,
                LastKnownPosition = CopyPosition(position),
                LastPerceivedTime = _elapsedTime,
                Strength = target.Strength
            };

            _entries[target] = updated;
        }
        else
        {
            if (_entries.Count >= MaxCapacity)
            {
                RemoveOldestEntry();
            }

            var entry = new PerceptionMemoryEntry
            {
                Target = target,
                LastKnownPosition = CopyPosition(position),
                LastPerceivedTime = _elapsedTime,
                Strength = target.Strength
            };

            _entries[target] = entry;
        }
    }

    /// <summary>
    /// 移除记忆条目
    /// </summary>
    /// <param name="target">目标刺激源</param>
    public void Remove(IAIStimulusSource target)
    {
        _entries.Remove(target);
    }

    /// <summary>
    /// 查询是否记忆中包含指定目标
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <returns>是否包含</returns>
    public bool Contains(IAIStimulusSource target)
    {
        return _entries.ContainsKey(target);
    }

    /// <summary>
    /// 获取指定目标的最后已知位置
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <returns>记忆条目，不存在则返回 null</returns>
    public PerceptionMemoryEntry? GetEntry(IAIStimulusSource target)
    {
        if (_entries.TryGetValue(target, out var entry))
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// 更新记忆（衰减过期条目）
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        _elapsedTime += delta;

        if (DecayTime <= 0)
        {
            return;
        }

        List<IAIStimulusSource>? expiredKeys = null;

        foreach (var kvp in _entries)
        {
            float timeSinceLastPerception = _elapsedTime - kvp.Value.LastPerceivedTime;

            if (timeSinceLastPerception > DecayTime)
            {
                expiredKeys ??= new List<IAIStimulusSource>();
                expiredKeys.Add(kvp.Key);
            }
        }

        if (expiredKeys is not null)
        {
            foreach (var key in expiredKeys)
            {
                _entries.Remove(key);
            }
        }
    }

    /// <summary>
    /// 清空记忆
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 移除最旧的记忆条目
    /// </summary>
    private void RemoveOldestEntry()
    {
        IAIStimulusSource? oldestKey = null;
        float oldestTime = float.MaxValue;

        foreach (var kvp in _entries)
        {
            if (kvp.Value.LastPerceivedTime < oldestTime)
            {
                oldestTime = kvp.Value.LastPerceivedTime;
                oldestKey = kvp.Key;
            }
        }

        if (oldestKey is not null)
        {
            _entries.Remove(oldestKey);
        }
    }

    /// <summary>
    /// 复制位置数组
    /// </summary>
    private static float[] CopyPosition(float[] position)
    {
        var copy = new float[position.Length];
        Array.Copy(position, copy, position.Length);
        return copy;
    }

    #endregion
}
