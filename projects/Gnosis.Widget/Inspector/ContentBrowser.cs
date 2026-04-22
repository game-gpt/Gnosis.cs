using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Inspector;

public sealed class ContentBrowser : ContainerElement
{
    #region 内部类型

    public sealed class BrowserItem
    {
        public string Name { get; }
        public string Path { get; }
        public bool IsDirectory { get; }
        public string? Extension { get; }

        public BrowserItem(string name, string path, bool isDirectory, string? extension = null)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
            Extension = extension;
        }
    }

    #endregion

    #region 属性

    private readonly List<BrowserItem> _items = [];
    private string _currentPath = "";
    private int _selectedIndex = -1;

    public IReadOnlyList<BrowserItem> Items => _items;

    public string CurrentPath => _currentPath;

    public float HeaderHeight { get; set; } = 28;

    public float ItemHeight { get; set; } = 22;

    public float ItemIconSize { get; set; } = 14;

    public float ItemSpacing { get; set; } = 4;

    public float ItemPadding { get; set; } = 4;

    public Color HeaderColor { get; set; } = new(0.18f, 0.18f, 0.22f, 1.0f);

    public Color HeaderTextColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    public Color SelectedColor { get; set; } = new(0.35f, 0.55f, 0.90f, 0.30f);

    public Color FolderIconColor { get; set; } = new(0.85f, 0.70f, 0.25f, 1.0f);

    public Color FileIconColor { get; set; } = new(0.50f, 0.60f, 0.80f, 1.0f);

    public Color ItemTextColor { get; set; } = new(0.80f, 0.80f, 0.84f, 1.0f);

    public float FontSize { get; set; } = 11;

    #endregion

    #region 事件

    public event Action<BrowserItem>? ItemSelected;
    public event Action<BrowserItem>? ItemDoubleClicked;
    public event Action<string>? PathChanged;

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var rect = LayoutRect.Deflate(Margin);

        renderer.DrawRect(
            rect.X, rect.Y,
            rect.Width, HeaderHeight,
            HeaderColor.R, HeaderColor.G, HeaderColor.B, HeaderColor.A
        );

        var pathDisplay = string.IsNullOrEmpty(_currentPath) ? "Assets" : _currentPath;
        renderer.DrawText(
            pathDisplay,
            rect.X + 10,
            rect.Y + (HeaderHeight - FontSize) / 2,
            FontSize,
            HeaderTextColor.R, HeaderTextColor.G, HeaderTextColor.B
        );

        renderer.PushClip(rect.X, rect.Y + HeaderHeight, rect.Width, rect.Height - HeaderHeight);

        var itemY = rect.Y + HeaderHeight + ItemPadding;

        for (var i = 0; i < _items.Count; i++)
        {
            if (itemY > rect.Bottom)
            {
                break;
            }

            var item = _items[i];

            if (i == _selectedIndex)
            {
                renderer.DrawRect(
                    rect.X + 2, itemY,
                    rect.Width - 4, ItemHeight,
                    SelectedColor.R, SelectedColor.G, SelectedColor.B, SelectedColor.A
                );
            }

            var iconColor = item.IsDirectory ? FolderIconColor : FileIconColor;

            renderer.DrawRect(
                rect.X + ItemPadding + 2, itemY + (ItemHeight - ItemIconSize) / 2,
                ItemIconSize, ItemIconSize,
                iconColor.R, iconColor.G, iconColor.B, iconColor.A
            );

            renderer.DrawText(
                item.Name,
                rect.X + ItemPadding + ItemIconSize + 6,
                itemY + (ItemHeight - FontSize) / 2,
                FontSize,
                ItemTextColor.R, ItemTextColor.G, ItemTextColor.B
            );

            itemY += ItemHeight + ItemSpacing;
        }

        renderer.PopClip();
    }

    #endregion

    #region 公开方法

    public void SetPath(string path)
    {
        _currentPath = path;
        _selectedIndex = -1;
        PathChanged?.Invoke(path);
    }

    public void SetItems(IEnumerable<BrowserItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = -1;
        InvalidateMeasure();
    }

    public void SelectItem(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _selectedIndex = index;
            ItemSelected?.Invoke(_items[index]);
        }
    }

    #endregion
}
