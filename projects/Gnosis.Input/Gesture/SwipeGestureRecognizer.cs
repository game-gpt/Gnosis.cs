using System.Diagnostics;
using System.Numerics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public sealed class SwipeGestureRecognizer : IGestureRecognizer
{
    #region 字段

    private bool _isTracking;
    private Vector2 _startPosition = Vector2.Zero;
    private long _startTimestamp;
    private readonly Stopwatch _stopwatch = new();

    #endregion

    #region 属性

    public bool IsEnabled { get; set; } = true;

    public float MinDistance { get; set; } = 50f;

    public float MaxDuration { get; set; } = 0.5f;

    #endregion

    #region 事件

    public event Action<SwipeDirection, float[], float>? OnSwipe;

    #endregion

    #region 构造函数

    public SwipeGestureRecognizer()
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
                StartTracking(new Vector2(touch.Position[0], touch.Position[1]));
                break;

            case TouchPhase.Ended when _isTracking:
                EndTracking(new Vector2(touch.Position[0], touch.Position[1]));
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
            StartTracking(position);
        }
        else if (!isPressed && _isTracking)
        {
            EndTracking(position);
        }
    }

    public void Reset()
    {
        _isTracking = false;
    }

    #endregion

    #region 私有方法

    private void StartTracking(Vector2 position)
    {
        _isTracking = true;
        _startPosition = position;
        _startTimestamp = _stopwatch.ElapsedMilliseconds;
    }

    private void EndTracking(Vector2 endPosition)
    {
        var elapsed = (_stopwatch.ElapsedMilliseconds - _startTimestamp) / 1000f;

        if (elapsed > MaxDuration)
        {
            _isTracking = false;
            return;
        }

        var dx = endPosition.X - _startPosition.X;
        var dy = endPosition.Y - _startPosition.Y;
        var distance = MathF.Sqrt(dx * dx + dy * dy);

        if (distance < MinDistance)
        {
            _isTracking = false;
            return;
        }

        var velocity = distance / elapsed;
        var direction = MathF.Abs(dx) > MathF.Abs(dy)
            ? (dx > 0 ? SwipeDirection.Right : SwipeDirection.Left)
            : (dy > 0 ? SwipeDirection.Down : SwipeDirection.Up);

        OnSwipe?.Invoke(direction, [endPosition.X, endPosition.Y], velocity);
        _isTracking = false;
    }

    #endregion
}
