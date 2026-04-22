namespace Gnosis.Input.Device;

public sealed class TouchDevice : ITouch
{
    #region 字段

    private readonly List<TouchPoint> _activeTouches = new();
    private readonly Dictionary<int, TouchPoint> _touchById = new();

    #endregion

    #region 属性

    public string Name => "Touch";

    public InputDeviceType DeviceType => InputDeviceType.Touch;

    public bool IsConnected { get; set; }

    public int TouchCount => _activeTouches.Count;

    #endregion

    #region ITouch 实现

    public TouchPoint GetTouch(int index)
    {
        if (index < 0 || index >= _activeTouches.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"触控索引超出范围：{index}");
        }

        return _activeTouches[index];
    }

    #endregion

    #region 内部方法

    public void UpdateTouch(int fingerId, float[] position, float[] deltaPosition, TouchPhase phase, float pressure)
    {
        var touchPoint = new TouchPoint
        {
            FingerId = fingerId,
            Position = position,
            DeltaPosition = deltaPosition,
            Phase = phase,
            Pressure = pressure
        };

        _touchById[fingerId] = touchPoint;
    }

    #endregion

    #region IInputDevice 实现

    public void Update()
    {
        _activeTouches.Clear();

        foreach (var kvp in _touchById)
        {
            if (kvp.Value.Phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                continue;
            }

            _activeTouches.Add(kvp.Value);
        }

        var keysToRemove = new List<int>();

        foreach (var kvp in _touchById)
        {
            if (kvp.Value.Phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _touchById.Remove(key);
        }
    }

    public void Reset()
    {
        _activeTouches.Clear();
        _touchById.Clear();
    }

    #endregion
}
