namespace Gnosis.Input.Cursor;

public sealed class CursorManager
{
    #region 字段

    private bool _isVisible = true;
    private bool _isLocked;
    private float[] _position = [0f, 0f];
    private float[] _lockedPosition = [0f, 0f];
    private CursorShape _currentShape = CursorShape.Default;

    #endregion

    #region 属性

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnCursorStateChanged?.Invoke(_position, _isVisible);
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnCursorStateChanged?.Invoke(_position, _isVisible);
            }
        }
    }

    public float[] Position => _position;

    public float[] LockedPosition
    {
        get => _lockedPosition;
        set => _lockedPosition = value ?? [0f, 0f];
    }

    public CursorShape CurrentShape => _currentShape;

    #endregion

    #region 事件

    public event Action<float[], bool>? OnCursorStateChanged;

    #endregion

    #region 公开方法

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
    }

    public void SetPosition(float x, float y)
    {
        _position = [x, y];
    }

    public void SetCustomShape(int shapeId)
    {
        _currentShape = CursorShape.Custom;
    }

    public void SetShape(CursorShape shape)
    {
        _currentShape = shape;
    }

    #endregion
}
