using Gnosis.Widget.Render;

namespace Gnosis.Widget.Element;

public abstract class WidgetElement : IWidgetElement
{
    #region 属性

    public string? Id { get; set; }

    public string? StyleClass { get; set; }

    public EdgeInsets Margin { get; set; } = EdgeInsets.Zero;

    public EdgeInsets Padding { get; set; } = EdgeInsets.Zero;

    public EdgeInsets Border { get; set; } = EdgeInsets.Zero;

    public Color BorderColor { get; set; } = Color.Transparent;

    public float? Width { get; set; }

    public float? Height { get; set; }

    public float MinWidth { get; set; } = 0;

    public float MinHeight { get; set; } = 0;

    public float MaxWidth { get; set; } = float.PositiveInfinity;

    public float MaxHeight { get; set; } = float.PositiveInfinity;

    public Color Background { get; set; } = Color.Transparent;

    public Color Foreground { get; set; } = Color.White;

    public Visibility Visibility { get; set; } = Visibility.Visible;

    public bool IsEnabled { get; set; } = true;

    public bool IsFocusable { get; set; } = false;

    public bool IsFocused { get; internal set; } = false;

    public WidgetElement? Parent { get; internal set; }

    IWidgetElement? IWidgetElement.Parent => Parent;

    #endregion

    #region 布局状态

    public Rect LayoutRect { get; internal set; } = Rect.Zero;

    public Size DesiredSize { get; internal set; } = Size.Zero;

    private bool _measureDirty = true;
    private bool _arrangeDirty = true;

    internal bool IsMeasureDirty() => _measureDirty;
    internal bool IsArrangeDirty() => _arrangeDirty;

    #endregion

    #region 布局方法

    public void Measure(Size availableSize)
    {
        if (Visibility == Visibility.Collapsed)
        {
            DesiredSize = Size.Zero;
            return;
        }

        if (!_measureDirty)
        {
            return;
        }

        var constrained = ConstrainSize(availableSize);

        var contentAvailable = constrained
            .Deflate(Margin)
            .Deflate(Border)
            .Deflate(Padding);

        var desired = MeasureOverride(contentAvailable);

        desired = desired
            .Inflate(Padding)
            .Inflate(Border)
            .Inflate(Margin);

        desired = desired.Clamp(
            new Size(MinWidth, MinHeight),
            new Size(MaxWidth, MaxHeight)
        );

        if (Width.HasValue)
        {
            desired = new Size(Width.Value, desired.Height);
        }

        if (Height.HasValue)
        {
            desired = new Size(desired.Width, Height.Value);
        }

        DesiredSize = desired;
        _measureDirty = false;
    }

    public void Arrange(Rect finalRect)
    {
        if (Visibility == Visibility.Collapsed)
        {
            return;
        }

        LayoutRect = finalRect;

        var contentRect = finalRect
            .Deflate(Margin)
            .Deflate(Border)
            .Deflate(Padding);

        ArrangeOverride(contentRect);

        _arrangeDirty = false;
    }

    protected abstract Size MeasureOverride(Size availableSize);

    protected abstract void ArrangeOverride(Rect contentRect);

    public void InvalidateMeasure()
    {
        _measureDirty = true;
        _arrangeDirty = true;
        Parent?.InvalidateMeasure();
    }

    public void InvalidateArrange()
    {
        _arrangeDirty = true;
    }

    private Size ConstrainSize(Size available)
    {
        return new Size(
            Math.Min(available.Width, MaxWidth),
            Math.Min(available.Height, MaxHeight)
        );
    }

    #endregion

    #region 命中测试

    public bool HitTest(float x, float y)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (Visibility != Visibility.Visible)
        {
            return false;
        }

        var contentRect = LayoutRect.Deflate(Margin);

        return x >= contentRect.X && x <= contentRect.Right &&
               y >= contentRect.Y && y <= contentRect.Bottom;
    }

    #endregion

    #region 事件系统

    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void AddHandler<T>(WidgetEventHandler<T> handler) where T : WidgetEventArgs
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
    }

    public void RemoveHandler<T>(WidgetEventHandler<T> handler) where T : WidgetEventArgs
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
        {
            list.Remove(handler);
        }
    }

    public void DispatchEvent<T>(T e) where T : WidgetEventArgs
    {
        if (!IsEnabled)
        {
            return;
        }

        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
        {
            foreach (var handler in list)
            {
                if (handler is WidgetEventHandler<T> typedHandler)
                {
                    typedHandler(this, e);
                    if (e.Handled)
                    {
                        return;
                    }
                }
            }
        }
    }

    #endregion

    #region 渲染

    public abstract void Paint(IWidgetRenderer renderer);

    #endregion
}
