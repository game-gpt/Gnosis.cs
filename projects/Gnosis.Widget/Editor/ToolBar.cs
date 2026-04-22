using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Editor;

public sealed class ToolBarItem
{
    public string Label { get; }

    public string? ShortcutHint { get; }

    public bool IsEnabled { get; set; } = true;

    public bool IsToggled { get; set; }

    public Action? OnClick { get; set; }

    public ToolBarItem(string label, string? shortcutHint = null)
    {
        Label = label;
        ShortcutHint = shortcutHint;
    }
}

public sealed class ToolBar : ContainerElement
{
    #region 属性

    private readonly List<object> _items = [];
    private int _hoveredIndex = -1;

    public float ItemPadding { get; set; } = 8;

    public float FontSize { get; set; } = 12;

    public Color HoverColor { get; set; } = new(0.30f, 0.30f, 0.34f, 1.0f);

    public Color ActiveColor { get; set; } = new(0.35f, 0.55f, 0.90f, 0.30f);

    public Color TextColor { get; set; } = new(0.85f, 0.85f, 0.88f, 1.0f);

    public Color SeparatorColor { get; set; } = new(0.35f, 0.35f, 0.38f, 1.0f);

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        return new Size(availableSize.Width, Height ?? 32);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var x = LayoutRect.X;
        var y = LayoutRect.Y;
        var height = Height ?? 32;

        float currentX = x + 4;
        var itemIndex = 0;

        foreach (var item in _items)
        {
            if (item is ToolBarSeparator)
            {
                renderer.DrawLine(
                    currentX, y + 4,
                    currentX, y + height - 4,
                    SeparatorColor.R, SeparatorColor.G, SeparatorColor.B
                );
                currentX += 8;
                continue;
            }

            var btn = (ToolBarItem)item;
            var labelWidth = btn.Label.Length * FontSize * 0.6f + ItemPadding * 2;

            if (itemIndex == _hoveredIndex)
            {
                renderer.DrawRect(
                    currentX, y + 2,
                    labelWidth, height - 4,
                    HoverColor.R, HoverColor.G, HoverColor.B, HoverColor.A
                );
            }

            if (btn.IsToggled)
            {
                renderer.DrawRect(
                    currentX, y + 2,
                    labelWidth, height - 4,
                    ActiveColor.R, ActiveColor.G, ActiveColor.B, ActiveColor.A
                );
            }

            renderer.DrawText(
                btn.Label,
                currentX + ItemPadding,
                y + (height - FontSize) / 2,
                FontSize,
                TextColor.R, TextColor.G, TextColor.B
            );

            currentX += labelWidth;
            itemIndex++;
        }
    }

    #endregion

    #region 公开方法

    public void AddItem(ToolBarItem item)
    {
        _items.Add(item);
    }

    public void AddSeparator()
    {
        _items.Add(new ToolBarSeparator());
    }

    #endregion

    private sealed class ToolBarSeparator;
}
