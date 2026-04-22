namespace Gnosis.Widget.Element;

public interface IWidgetElement
{
    string? Id { get; set; }
    string? StyleClass { get; set; }
    EdgeInsets Margin { get; set; }
    EdgeInsets Padding { get; set; }
    EdgeInsets Border { get; set; }
    Color BorderColor { get; set; }
    float? Width { get; set; }
    float? Height { get; set; }
    float MinWidth { get; set; }
    float MinHeight { get; set; }
    float MaxWidth { get; set; }
    float MaxHeight { get; set; }
    Color Background { get; set; }
    Color Foreground { get; set; }
    Visibility Visibility { get; set; }
    bool IsEnabled { get; set; }
    bool IsFocusable { get; set; }
    bool IsFocused { get; }
    IWidgetElement? Parent { get; }
    Rect LayoutRect { get; }
    Size DesiredSize { get; }

    void Measure(Size availableSize);
    void Arrange(Rect finalRect);
    void InvalidateMeasure();
    void InvalidateArrange();
    bool HitTest(float x, float y);
}
