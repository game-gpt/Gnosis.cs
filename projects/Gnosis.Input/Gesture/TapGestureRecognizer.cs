using System.Diagnostics;
using System.Numerics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public sealed class TapGestureRecognizer : IGestureRecognizer
{
    #region 字段

    private bool _isTracking;
    private Vector2 _startPosition = Vector2.Zero;
    private long _startTimestamp;
    private readonly Stopwatch _stopwatch = new();

    #endregion

    #region 属性

    public bool IsEnabled { get; set; } = true;

    public float MaxDuration { get; set; } = 0.3f;

    public float MaxDistance { get; set; } = 10f;

    #endregion

    #region 事件

    public event Action<float[], float>? OnTap;

    #endregion

    #region 构造函数

    public TapGestureRecognizer()
    {
        _stopwatch.Start();
    }

    #endregion

    #region IGestureRecognizer 实现

    public void ProcessTouch(in TouchPoint touch)
    {
        if (!IsEnabled)
        {
            return;
        }

        switch (touch.Phase)
        {
            case TouchPhase.Began:
                _isTracking = true;
                _startPosition = new Vector2(touch.Position[0], touch.Position[1]);
                _startTimestamp = _stopwatch.ElapsedMilliseconds;
                break;

            case TouchPhase.Ended when _isTracking:
                var elapsed = (_stopwatch.ElapsedMilliseconds - _startTimestamp) / 1000f;

                if (elapsed <= MaxDuration)
                {
                    var dx = touch.Position[0] - _startPosition[0];
                    var dy = touch.Position[1] - _startPosition[1];
                    var distance = MathF.Sqrt(dx * dx + dy * dy);

                    if (distance <= MaxDistance)
                    {
                        OnTap?.Invoke(touch.Position, elapsed);
                    }
                }

                _isTracking = false;
                break;

            case TouchPhase.Canceled:
                _isTracking = false;
                break;
        }
    }

    public void ProcessMouse(Vector2 position, bool isPressed)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (isPressed && !_isTracking)
        {
            _isTracking = true;
            _startPosition = position;
            _startTimestamp = _stopwatch.ElapsedMilliseconds;
        }
        else if (!isPressed && _isTracking)
        {
            var elapsed = (_stopwatch.ElapsedMilliseconds - _startTimestamp) / 1000f;

            if (elapsed <= MaxDuration)
            {
                var dx = position.X - _startPosition.X;
                var dy = position.Y - _startPosition.Y;
                var distance = MathF.Sqrt(dx * dx + dy * dy);

                if (distance <= MaxDistance)
                {
                    OnTap?.Invoke([position.X, position.Y], elapsed);
                }
            }

            _isTracking = false;
        }
    }

    public void Reset()
    {
        _isTracking = false;
    }

    #endregion
}
