using Gnosis.Widget.Control;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;
using Gnosis.Widget.Scene;

namespace Gnosis.Widget.Inspector;

public sealed class InspectorPanel : ContainerElement
{
    #region 属性

    private readonly ScrollView _scrollView;
    private readonly VBox _content;

    public float HeaderHeight { get; set; } = 28;

    public Color HeaderColor { get; set; } = new(0.18f, 0.18f, 0.22f, 1.0f);

    public Color HeaderTextColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    public float HeaderFontSize { get; set; } = 13;

    public Color SectionHeaderColor { get; set; } = new(0.80f, 0.80f, 0.84f, 1.0f);

    public float SectionHeaderFontSize { get; set; } = 12;

    public Color LabelColor { get; set; } = new(0.65f, 0.65f, 0.69f, 1.0f);

    public float LabelFontSize { get; set; } = 11;

    #endregion

    #region 构造

    public InspectorPanel()
    {
        _content = new VBox
        {
            Padding = new EdgeInsets(8, 8, 8, 8),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        _scrollView = new ScrollView
        {
            ShowVerticalScrollBar = true
        };
        _scrollView.AddChild(_content);

        AddChild(_scrollView);
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var contentAvailable = new Size(
            availableSize.Width,
            Math.Max(0, availableSize.Height - HeaderHeight)
        );

        _scrollView.Measure(contentAvailable);

        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        _scrollView.Arrange(new Rect(
            contentRect.X,
            contentRect.Y + HeaderHeight,
            contentRect.Width,
            Math.Max(0, contentRect.Height - HeaderHeight)
        ));
    }

    #endregion

    #region 渲染

    protected override void PaintBackground(IWidgetRenderer renderer, Rect contentRect)
    {
        renderer.DrawRect(
            contentRect.X, contentRect.Y,
            contentRect.Width, HeaderHeight,
            HeaderColor.R, HeaderColor.G, HeaderColor.B, HeaderColor.A
        );

        renderer.DrawText(
            "Inspector",
            contentRect.X + 10,
            contentRect.Y + (HeaderHeight - HeaderFontSize) / 2,
            HeaderFontSize,
            HeaderTextColor.R, HeaderTextColor.G, HeaderTextColor.B
        );
    }

    #endregion

    #region 公开方法

    public void InspectEntity(EntityData? entity)
    {
        _content.ClearChildren();

        if (entity == null)
        {
            return;
        }

        var nameSection = new VBox { CrossAxisAlignment = CrossAxisAlignment.Stretch };
        nameSection.AddChild(new TextWidget("Name") { FontSize = SectionHeaderFontSize, Foreground = SectionHeaderColor });
        nameSection.AddChild(new TextWidget($"  {entity.Name}") { FontSize = LabelFontSize, Foreground = LabelColor });
        _content.AddChild(nameSection);

        _content.AddChild(new SeparatorWidget { Margin = new EdgeInsets(4, 0, 4, 0) });

        var transformSection = new VBox { CrossAxisAlignment = CrossAxisAlignment.Stretch };
        transformSection.AddChild(new TextWidget("Transform") { FontSize = SectionHeaderFontSize, Foreground = SectionHeaderColor });
        transformSection.AddChild(new TextWidget($"  Position: {entity.PositionX:F2}, {entity.PositionY:F2}, {entity.PositionZ:F2}") { FontSize = LabelFontSize, Foreground = LabelColor });
        transformSection.AddChild(new TextWidget($"  Rotation: {entity.RotationX:F2}, {entity.RotationY:F2}, {entity.RotationZ:F2}") { FontSize = LabelFontSize, Foreground = LabelColor });
        transformSection.AddChild(new TextWidget($"  Scale: {entity.ScaleX:F2}, {entity.ScaleY:F2}, {entity.ScaleZ:F2}") { FontSize = LabelFontSize, Foreground = LabelColor });
        _content.AddChild(transformSection);

        foreach (var component in entity.Components)
        {
            _content.AddChild(new SeparatorWidget { Margin = new EdgeInsets(4, 0, 4, 0) });

            var compSection = new VBox { CrossAxisAlignment = CrossAxisAlignment.Stretch };
            compSection.AddChild(new TextWidget(component.TypeName) { FontSize = SectionHeaderFontSize, Foreground = SectionHeaderColor });
            _content.AddChild(compSection);
        }

        InvalidateMeasure();
    }

    #endregion
}
