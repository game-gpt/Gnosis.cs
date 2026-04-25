using System.Numerics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public sealed class PinchGestureRecognizer : IGestureRecognizer
{
    #region 字段

    private float _previousDistance;
    private bool _isTracking;

    #endregion

    #region 属性

    public bool IsEnabled { get; set; } = true;

    public float MinScaleDelta { get; set; } = 0.01f;

    #endregion

    #region 事件

    public event Action<float, float[]>? OnPinch;

    #endregion

    #region IGestureRecognizer 实现

    public void ProcessTouch(in TouchPoint touch)
    {
        if (!IsEnabled)
        {
            return;
        }
    }

    public void ProcessMouse(Vector2 position, bool isPressed)
    {
    }

    public void Reset()
    {
        _isTracking = false;
        _previousDistance = 0f;
    }

    #endregion

    #region 内部方法

    public void ProcessTwoTouches(in TouchPoint touch0, in TouchPoint touch1)
    {
        if (!IsEnabled)
        {
            return;
        }

        var dx = touch1.Position[0] - touch0.Position[0];
        var dy = touch1.Position[1] - touch0.Position[1];
        var currentDistance = MathF.Sqrt(dx * dx + dy * dy);

        var centerX = (touch0.Position[0] + touch1.Position[0]) / 2f;
        var centerY = (touch0.Position[1] + touch1.Position[1]) / 2f;

        if (!_isTracking)
        {
            _isTracking = true;
            _previousDistance = currentDistance;
            return;
        }

        if (_previousDistance > 0.001f)
        {
            var scaleDelta = currentDistance / _previousDistance;

            if (MathF.Abs(scaleDelta - 1f) >= MinScaleDelta)
            {
                OnPinch?.Invoke(scaleDelta, [centerX, centerY]);
            }
        }

        _previousDistance = currentDistance;
    }

    #endregion
}
