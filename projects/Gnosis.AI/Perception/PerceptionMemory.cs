namespace Gnosis.AI.Perception;

public sealed class PerceptionMemory : IPerceptionMemory
{
    #region 字段

    private readonly Dictionary<IAIStimulusSource, PerceptionMemoryEntry> _entries = new();
    private float _elapsedTime;

    #endregion

    #region 属性

    public IReadOnlyList<PerceptionMemoryEntry> Entries => _entries.Values.ToList().AsReadOnly();
    public int MaxCapacity { get; }
    public float DecayTime { get; set; }
    public int Count => _entries.Count;

    #endregion

    #region 构造函数

    public PerceptionMemory(int maxCapacity = 16, float decayTime = 5.0f)
    {
        MaxCapacity = maxCapacity;
        DecayTime = decayTime;
    }

    #endregion

    #region 公有方法

    public void AddOrUpdate(IAIStimulusSource target, Vector3 position)
    {
        if (_entries.TryGetValue(target, out _))
        {
            var updated = new PerceptionMemoryEntry
            {
                Target = target,
                LastKnownPosition = position,
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
                LastKnownPosition = position,
                LastPerceivedTime = _elapsedTime,
                Strength = target.Strength
            };

            _entries[target] = entry;
        }
    }

    public void Remove(IAIStimulusSource target)
    {
        _entries.Remove(target);
    }

    public bool Contains(IAIStimulusSource target)
    {
        return _entries.ContainsKey(target);
    }

    public PerceptionMemoryEntry? GetEntry(IAIStimulusSource target)
    {
        if (_entries.TryGetValue(target, out var entry))
        {
            return entry;
        }

        return null;
    }

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
            var timeSinceLastPerception = _elapsedTime - kvp.Value.LastPerceivedTime;

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

    public void Clear()
    {
        _entries.Clear();
    }

    #endregion

    #region 私有方法

    private void RemoveOldestEntry()
    {
        IAIStimulusSource? oldestKey = null;
        var oldestTime = float.MaxValue;

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

    #endregion
}
