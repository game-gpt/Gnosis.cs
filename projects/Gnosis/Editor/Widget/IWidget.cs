namespace Gnosis.Editor.Widget;

public interface IWidget
{
    string? Id { get; set; }
    WidgetEdgeInsets Margin { get; set; }
    WidgetEdgeInsets Padding { get; set; }
    float? Width { get; set; }
    float? Height { get; set; }
    float MinWidth { get; set; }
    float MinHeight { get; set; }
    float MaxWidth { get; set; }
    float MaxHeight { get; set; }
    WidgetColor Background { get; set; }
    WidgetColor Foreground { get; set; }
    WidgetVisibility Visibility { get; set; }
    IWidget? Parent { get; }
    WidgetRect LayoutRect { get; }
    WidgetSize DesiredSize { get; }

    void Measure(WidgetSize availableSize);
    void Arrange(WidgetRect finalRect);
    void InvalidateMeasure();
    void InvalidateArrange();
}
