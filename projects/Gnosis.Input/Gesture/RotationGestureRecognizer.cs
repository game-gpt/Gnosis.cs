using System.Numerics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public sealed class RotationGestureRecognizer : IGestureRecognizer
{
    #region 字段

    private float _previousAngle;
    private bool _isTracking;

    #endregion

    #region 属性

    public bool IsEnabled { get; set; } = true;

    public float MinAngleDelta { get; set; } = 1f;

    #endregion

    #region 事件

    public event Action<float, float[]>? OnRotate;

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
        _previousAngle = 0f;
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
        var currentAngle = MathF.Atan2(dy, dx) * (180f / MathF.PI);

        var centerX = (touch0.Position[0] + touch1.Position[0]) / 2f;
        var centerY = (touch0.Position[1] + touch1.Position[1]) / 2f;

        if (!_isTracking)
        {
            _isTracking = true;
            _previousAngle = currentAngle;
            return;
        }

        var angleDelta = currentAngle - _previousAngle;

        if (angleDelta > 180f)
        {
            angleDelta -= 360f;
        }
        else if (angleDelta < -180f)
        {
            angleDelta += 360f;
        }

        if (MathF.Abs(angleDelta) >= MinAngleDelta)
        {
            OnRotate?.Invoke(angleDelta, [centerX, centerY]);
        }

        _previousAngle = currentAngle;
    }

    #endregion
}
