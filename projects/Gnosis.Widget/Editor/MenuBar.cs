using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Editor;

public sealed class MenuItem
{
    #region 属性

    public string Label { get; }

    public string? Shortcut { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsChecked { get; set; }

    public bool IsSeparator { get; }

    public IReadOnlyList<MenuItem> Children => _children;

    public Action? OnClick { get; set; }

    #endregion

    #region 字段

    private readonly List<MenuItem> _children = [];

    #endregion

    #region 构造

    public MenuItem(string label)
    {
        Label = label;
    }

    private MenuItem(bool isSeparator)
    {
        IsSeparator = isSeparator;
        Label = "";
    }

    #endregion

    #region 公开方法

    public static MenuItem Separator() => new(true);

    public void AddChild(MenuItem item)
    {
        _children.Add(item);
    }

    public void RemoveChild(MenuItem item)
    {
        _children.Remove(item);
    }

    #endregion
}

public sealed class MenuBar : ContainerElement
{
    #region 属性

    private readonly List<MenuItem> _menus = [];
    private int _hoveredIndex = -1;
    private int _openIndex = -1;

    public IReadOnlyList<MenuItem> Menus => _menus;

    public float ItemPadding { get; set; } = 12;

    public float FontSize { get; set; } = 13;

    public Color HoverColor { get; set; } = new(0.30f, 0.30f, 0.34f, 1.0f);

    public Color OpenColor { get; set; } = new(0.25f, 0.25f, 0.28f, 1.0f);

    public Color TextColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        return new Size(availableSize.Width, Height ?? 28);
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
        var height = Height ?? 28;

        float currentX = x + 4;

        for (var i = 0; i < _menus.Count; i++)
        {
            var menu = _menus[i];
            var labelWidth = menu.Label.Length * FontSize * 0.6f + ItemPadding * 2;

            if (i == _hoveredIndex || i == _openIndex)
            {
                var bgColor = i == _openIndex ? OpenColor : HoverColor;
                renderer.DrawRect(
                    currentX, y,
                    labelWidth, height,
                    bgColor.R, bgColor.G, bgColor.B, bgColor.A
                );
            }

            renderer.DrawText(
                menu.Label,
                currentX + ItemPadding,
                y + (height - FontSize) / 2,
                FontSize,
                TextColor.R, TextColor.G, TextColor.B
            );

            currentX += labelWidth;
        }
    }

    #endregion

    #region 公开方法

    public void AddMenu(MenuItem menu)
    {
        _menus.Add(menu);
    }

    public void RemoveMenu(MenuItem menu)
    {
        _menus.Remove(menu);
    }

    #endregion
}
