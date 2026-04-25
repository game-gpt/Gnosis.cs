namespace Gnosis.Network.Prediction;

public interface IStateSnapshot
{
    int Frame { get; }
    byte[] Serialize();
}

public sealed class StateSnapshotBuffer<TSnapshot> where TSnapshot : IStateSnapshot
{
    #region 字段

    private readonly Dictionary<int, TSnapshot> _snapshots = new();
    private readonly int _maxBufferSize;
    private int _oldestFrame;
    private int _newestFrame;

    #endregion

    #region 属性

    public int Count => _snapshots.Count;

    public int OldestFrame => _oldestFrame;

    public int NewestFrame => _newestFrame;

    public int MaxBufferSize => _maxBufferSize;

    #endregion

    #region 构造函数

    public StateSnapshotBuffer(int maxBufferSize = 64)
    {
        _maxBufferSize = Math.Max(1, maxBufferSize);
        _oldestFrame = int.MaxValue;
        _newestFrame = int.MinValue;
    }

    #endregion

    #region 公开方法

    public void Record(int frame, TSnapshot snapshot)
    {
        _snapshots[frame] = snapshot;

        if (frame < _oldestFrame)
        {
            _oldestFrame = frame;
        }

        if (frame > _newestFrame)
        {
            _newestFrame = frame;
        }

        TrimOldSnapshots();
    }

    public TSnapshot? Get(int frame)
    {
        return _snapshots.TryGetValue(frame, out var snapshot) ? snapshot : default;
    }

    public TSnapshot? GetNearest(int frame)
    {
        if (_snapshots.TryGetValue(frame, out var exact))
        {
            return exact;
        }

        var nearestFrame = -1;
        var nearestDistance = int.MaxValue;

        foreach (var key in _snapshots.Keys)
        {
            var distance = Math.Abs(key - frame);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestFrame = key;
            }
        }

        return nearestFrame >= 0 ? _snapshots[nearestFrame] : default;
    }

    public bool HasSnapshot(int frame)
    {
        return _snapshots.ContainsKey(frame);
    }

    public void ClearBefore(int frame)
    {
        var keysToRemove = new List<int>();

        foreach (var key in _snapshots.Keys)
        {
            if (key < frame)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _snapshots.Remove(key);
        }

        UpdateFrameBounds();
    }

    public void Clear()
    {
        _snapshots.Clear();
        _oldestFrame = int.MaxValue;
        _newestFrame = int.MinValue;
    }

    public IReadOnlyList<TSnapshot> GetRange(int fromFrame, int toFrame)
    {
        var result = new List<TSnapshot>();

        for (int frame = fromFrame; frame <= toFrame; frame++)
        {
            if (_snapshots.TryGetValue(frame, out var snapshot))
            {
                result.Add(snapshot);
            }
        }

        return result;
    }

    #endregion

    #region 私有方法

    private void TrimOldSnapshots()
    {
        while (_snapshots.Count > _maxBufferSize && _oldestFrame < _newestFrame)
        {
            _snapshots.Remove(_oldestFrame);
            UpdateFrameBounds();
        }
    }

    private void UpdateFrameBounds()
    {
        _oldestFrame = int.MaxValue;
        _newestFrame = int.MinValue;

        foreach (var key in _snapshots.Keys)
        {
            if (key < _oldestFrame)
            {
                _oldestFrame = key;
            }

            if (key > _newestFrame)
            {
                _newestFrame = key;
            }
        }

        if (_snapshots.Count == 0)
        {
            _oldestFrame = 0;
            _newestFrame = 0;
        }
    }

    #endregion
}
