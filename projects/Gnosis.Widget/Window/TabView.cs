using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Window;

public sealed class TabView : ContainerElement
{
    #region 属性

    private int _selectedIndex;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= 0 && value < Children.Count)
            {
                _selectedIndex = value;
            }
        }
    }

    public float TabBarHeight { get; set; } = 28;

    public Color TabBarColor { get; set; } = new(0.15f, 0.15f, 0.18f, 1.0f);

    public Color TabActiveColor { get; set; } = new(0.20f, 0.20f, 0.24f, 1.0f);

    public Color TabHoverColor { get; set; } = new(0.22f, 0.22f, 0.26f, 1.0f);

    public Color TabTextColor { get; set; } = new(0.70f, 0.70f, 0.74f, 1.0f);

    public Color TabActiveTextColor { get; set; } = new(0.95f, 0.95f, 0.97f, 1.0f);

    public List<string> TabTitles { get; } = [];

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var contentAvailable = new Size(
            availableSize.Width,
            Math.Max(0, availableSize.Height - TabBarHeight)
        );

        float maxWidth = 0;
        float maxHeight = TabBarHeight;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(contentAvailable);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight += child.DesiredSize.Height;
        }

        return new Size(maxWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        var contentArea = new Rect(
            contentRect.X,
            contentRect.Y + TabBarHeight,
            contentRect.Width,
            Math.Max(0, contentRect.Height - TabBarHeight)
        );

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];

            if (i == _selectedIndex)
            {
                child.Visibility = Visibility.Visible;
                child.Arrange(contentArea);
            }
            else
            {
                child.Visibility = Visibility.Hidden;
                child.Arrange(Rect.Zero);
            }
        }
    }

    #endregion

    #region 渲染

    protected override void PaintBackground(IWidgetRenderer renderer, Rect contentRect)
    {
        renderer.DrawRect(
            contentRect.X, contentRect.Y,
            contentRect.Width, TabBarHeight,
            TabBarColor.R, TabBarColor.G, TabBarColor.B, TabBarColor.A
        );

        var tabWidth = contentRect.Width / Math.Max(1, TabTitles.Count);

        for (var i = 0; i < TabTitles.Count; i++)
        {
            var tabX = contentRect.X + i * tabWidth;

            var tabColor = i == _selectedIndex ? TabActiveColor : TabBarColor;

            renderer.DrawRect(
                tabX, contentRect.Y,
                tabWidth, TabBarHeight,
                tabColor.R, tabColor.G, tabColor.B, tabColor.A
            );

            var textColor = i == _selectedIndex ? TabActiveTextColor : TabTextColor;

            renderer.DrawText(
                TabTitles[i],
                tabX + 8,
                contentRect.Y + 6,
                12,
                textColor.R, textColor.G, textColor.B
            );
        }
    }

    #endregion

    #region 公开方法

    public void AddTab(string title, WidgetElement content)
    {
        TabTitles.Add(title);
        AddChild(content);
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= TabTitles.Count)
        {
            return;
        }

        TabTitles.RemoveAt(index);
        RemoveChild(Children[index]);

        if (_selectedIndex >= TabTitles.Count)
        {
            _selectedIndex = Math.Max(0, TabTitles.Count - 1);
        }
    }

    #endregion
}
