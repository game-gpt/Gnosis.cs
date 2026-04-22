using Gnosis.Widget.Control;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Scene;

public sealed class HierarchyPanel : ContainerElement
{
    #region 属性

    private readonly TreeView _treeView;
    private readonly ScrollView _scrollView;

    public TreeView Tree => _treeView;

    public float HeaderHeight { get; set; } = 28;

    public Color HeaderColor { get; set; } = new(0.18f, 0.18f, 0.22f, 1.0f);

    public Color HeaderTextColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    public float HeaderFontSize { get; set; } = 13;

    #endregion

    #region 构造

    public HierarchyPanel()
    {
        _treeView = new TreeView
        {
            Foreground = new Color(0.70f, 0.70f, 0.74f, 1.0f)
        };

        _scrollView = new ScrollView
        {
            ShowVerticalScrollBar = true
        };
        _scrollView.AddChild(_treeView);

        AddChild(_scrollView);
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var headerH = HeaderHeight;
        var contentAvailable = new Size(
            availableSize.Width,
            Math.Max(0, availableSize.Height - headerH)
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
            "Hierarchy",
            contentRect.X + 10,
            contentRect.Y + (HeaderHeight - HeaderFontSize) / 2,
            HeaderFontSize,
            HeaderTextColor.R, HeaderTextColor.G, HeaderTextColor.B
        );
    }

    #endregion

    #region 公开方法

    public void SetSceneTree(TreeNode root)
    {
        _treeView.Clear();
        _treeView.AddRoot(root);
    }

    #endregion
}
