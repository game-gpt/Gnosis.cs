using Gnosis;
using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public enum Visibility
{
    Visible,
    Hidden,
    Collapsed
}

public abstract class Widget : IWidget
{
    #region Properties

    public string? Id { get; set; }

    public EdgeInsets Margin { get; set; } = EdgeInsets.Zero;

    public EdgeInsets Padding { get; set; } = EdgeInsets.Zero;

    public float? Width { get; set; }

    public float? Height { get; set; }

    public float MinWidth { get; set; } = 0;

    public float MinHeight { get; set; } = 0;

    public float MaxWidth { get; set; } = float.PositiveInfinity;

    public float MaxHeight { get; set; } = float.PositiveInfinity;

    public Color Background { get; set; } = Color.Transparent;

    public Color Foreground { get; set; } = Color.White;

    public Visibility Visibility { get; set; } = Visibility.Visible;

    public Widget? Parent { get; internal set; }

    #endregion

    #region IWidget Explicit Implementation

    WidgetEdgeInsets IWidget.Margin
    {
        get => new WidgetEdgeInsets(Margin.Top, Margin.Right, Margin.Bottom, Margin.Left);
        set => Margin = new EdgeInsets(value.Top, value.Right, value.Bottom, value.Left);
    }

    WidgetEdgeInsets IWidget.Padding
    {
        get => new WidgetEdgeInsets(Padding.Top, Padding.Right, Padding.Bottom, Padding.Left);
        set => Padding = new EdgeInsets(value.Top, value.Right, value.Bottom, value.Left);
    }

    WidgetColor IWidget.Background
    {
        get => new WidgetColor(Background.R, Background.G, Background.B, Background.A);
        set => Background = new Color(value.R, value.G, value.B, value.A);
    }

    WidgetColor IWidget.Foreground
    {
        get => new WidgetColor(Foreground.R, Foreground.G, Foreground.B, Foreground.A);
        set => Foreground = new Color(value.R, value.G, value.B, value.A);
    }

    WidgetVisibility IWidget.Visibility
    {
        get => Visibility switch
        {
            UI.Visibility.Visible => WidgetVisibility.Visible,
            UI.Visibility.Hidden => WidgetVisibility.Hidden,
            UI.Visibility.Collapsed => WidgetVisibility.Collapsed,
            _ => WidgetVisibility.Visible
        };
        set => Visibility = value switch
        {
            WidgetVisibility.Visible => UI.Visibility.Visible,
            WidgetVisibility.Hidden => UI.Visibility.Hidden,
            WidgetVisibility.Collapsed => UI.Visibility.Collapsed,
            _ => UI.Visibility.Visible
        };
    }

    IWidget? IWidget.Parent => Parent;

    WidgetRect IWidget.LayoutRect => new WidgetRect(LayoutRect.X, LayoutRect.Y, LayoutRect.Width, LayoutRect.Height);

    WidgetSize IWidget.DesiredSize => new WidgetSize(DesiredSize.Width, DesiredSize.Height);

    #endregion

    #region Layout State

    public Rect LayoutRect { get; internal set; } = Rect.Zero;

    public Size DesiredSize { get; internal set; } = Size.Zero;

    private bool _measureDirty = true;
    private bool _arrangeDirty = true;

    internal bool IsMeasureDirty() => _measureDirty;
    internal bool IsArrangeDirty() => _arrangeDirty;

    #endregion

    #region Layout Methods

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

        var contentAvailable = constrained.Deflate(Margin).Deflate(Padding);

        var desired = MeasureOverride(contentAvailable);

        desired = desired.Inflate(Padding).Inflate(Margin);

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

    void IWidget.Measure(WidgetSize availableSize)
    {
        Measure(new Size(availableSize.Width, availableSize.Height));
    }

    public void Arrange(Rect finalRect)
    {
        if (Visibility == Visibility.Collapsed)
        {
            return;
        }

        LayoutRect = finalRect;

        var contentRect = finalRect.Deflate(Margin).Deflate(Padding);

        ArrangeOverride(contentRect);

        _arrangeDirty = false;
    }

    void IWidget.Arrange(WidgetRect finalRect)
    {
        Arrange(new Rect(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height));
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

    #region Rendering

    public abstract void Paint(UiRenderer renderer);

    #endregion
}

public readonly struct Color
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public static Color Transparent { get; } = new(0, 0, 0, 0);
    public static Color White { get; } = new(1, 1, 1, 1);
    public static Color Black { get; } = new(0, 0, 0, 1);

    public static Color FromRgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1.0f);

    public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}
