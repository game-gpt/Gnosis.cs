namespace Gnosis.Network.Prediction;

public interface IInputPayload
{
    int Frame { get; }
    byte[] Serialize();
}

public sealed class InputBuffer<TInput> where TInput : IInputPayload
{
    #region 字段

    private readonly Dictionary<int, TInput> _inputs = new();
    private readonly int _maxBufferSize;

    #endregion

    #region 属性

    public int Count => _inputs.Count;

    public int MaxBufferSize => _maxBufferSize;

    public int OldestFrame { get; private set; }

    public int NewestFrame { get; private set; }

    #endregion

    #region 构造函数

    public InputBuffer(int maxBufferSize = 128)
    {
        _maxBufferSize = Math.Max(1, maxBufferSize);
        OldestFrame = int.MaxValue;
        NewestFrame = int.MinValue;
    }

    #endregion

    #region 公开方法

    public void Record(int frame, TInput input)
    {
        _inputs[frame] = input;

        if (frame < OldestFrame)
        {
            OldestFrame = frame;
        }

        if (frame > NewestFrame)
        {
            NewestFrame = frame;
        }

        TrimOldInputs();
    }

    public TInput? Get(int frame)
    {
        return _inputs.TryGetValue(frame, out var input) ? input : default;
    }

    public bool HasInput(int frame)
    {
        return _inputs.ContainsKey(frame);
    }

    public IReadOnlyList<TInput> GetRange(int fromFrame, int toFrame)
    {
        var result = new List<TInput>();

        for (int frame = fromFrame; frame <= toFrame; frame++)
        {
            if (_inputs.TryGetValue(frame, out var input))
            {
                result.Add(input);
            }
        }

        return result;
    }

    public void ClearBefore(int frame)
    {
        var keysToRemove = new List<int>();

        foreach (var key in _inputs.Keys)
        {
            if (key < frame)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _inputs.Remove(key);
        }

        UpdateFrameBounds();
    }

    public void Clear()
    {
        _inputs.Clear();
        OldestFrame = int.MaxValue;
        NewestFrame = int.MinValue;
    }

    #endregion

    #region 私有方法

    private void TrimOldInputs()
    {
        while (_inputs.Count > _maxBufferSize && OldestFrame < NewestFrame)
        {
            _inputs.Remove(OldestFrame);
            UpdateFrameBounds();
        }
    }

    private void UpdateFrameBounds()
    {
        OldestFrame = int.MaxValue;
        NewestFrame = int.MinValue;

        foreach (var key in _inputs.Keys)
        {
            if (key < OldestFrame)
            {
                OldestFrame = key;
            }

            if (key > NewestFrame)
            {
                NewestFrame = key;
            }
        }

        if (_inputs.Count == 0)
        {
            OldestFrame = 0;
            NewestFrame = 0;
        }
    }

    #endregion
}
