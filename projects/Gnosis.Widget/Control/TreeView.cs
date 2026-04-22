using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Control;

public sealed class TreeNode
{
    public string Text { get; set; } = "";

    public IReadOnlyList<TreeNode> Children { get; }

    public bool IsExpanded { get; set; }

    public bool IsSelected { get; set; }

    public object? Tag { get; set; }

    private readonly List<TreeNode> _children = [];

    public TreeNode(string text, object? tag = null)
    {
        Text = text;
        Tag = tag;
        Children = _children;
    }

    public void AddChild(TreeNode child)
    {
        _children.Add(child);
    }

    public void RemoveChild(TreeNode child)
    {
        _children.Remove(child);
    }
}

public sealed class TreeView : WidgetElement
{
    #region 属性

    private readonly List<TreeNode> _roots = [];

    public IReadOnlyList<TreeNode> Roots => _roots;

    public float IndentSize { get; set; } = 16;

    public float RowHeight { get; set; } = 20;

    public float FontSize { get; set; } = 12;

    public float ExpandIconSize { get; set; } = 8;

    public Color ExpandIconColor { get; set; } = new(0.60f, 0.60f, 0.64f, 1.0f);

    public Color SelectedColor { get; set; } = new(0.35f, 0.55f, 0.90f, 0.30f);

    public Color HoverColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.06f);

    public TreeNode? SelectedNode { get; set; }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        var totalHeight = CountVisibleNodes() * RowHeight;

        return new Size(
            Math.Min(availableSize.Width, 300),
            Math.Min(totalHeight, availableSize.Height)
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var x = LayoutRect.X;
        var y = LayoutRect.Y;

        renderer.PushClip(LayoutRect.X, LayoutRect.Y, LayoutRect.Width, LayoutRect.Height);

        PaintNodes(renderer, _roots, x, ref y, 0);

        renderer.PopClip();
    }

    private void PaintNodes(IWidgetRenderer renderer, IReadOnlyList<TreeNode> nodes, float x, ref float y, int depth)
    {
        foreach (var node in nodes)
        {
            if (y > LayoutRect.Bottom)
            {
                break;
            }

            var indent = x + depth * IndentSize;

            if (node.IsSelected)
            {
                renderer.DrawRect(
                    x, y,
                    LayoutRect.Width, RowHeight,
                    SelectedColor.R, SelectedColor.G, SelectedColor.B, SelectedColor.A
                );
            }

            if (node.Children.Count > 0)
            {
                PaintExpandIcon(renderer, indent + 2, y + (RowHeight - ExpandIconSize) / 2, node.IsExpanded);
            }

            var textX = indent + ExpandIconSize + 6;
            renderer.DrawText(
                node.Text,
                textX,
                y + (RowHeight - FontSize) / 2,
                FontSize,
                Foreground.R, Foreground.G, Foreground.B
            );

            y += RowHeight;

            if (node.IsExpanded && node.Children.Count > 0)
            {
                PaintNodes(renderer, node.Children, x, ref y, depth + 1);
            }
        }
    }

    private void PaintExpandIcon(IWidgetRenderer renderer, float x, float y, bool expanded)
    {
        if (expanded)
        {
            renderer.DrawLine(
                x, y,
                x + ExpandIconSize, y,
                ExpandIconColor.R, ExpandIconColor.G, ExpandIconColor.B
            );
            renderer.DrawLine(
                x + ExpandIconSize / 2, y,
                x + ExpandIconSize / 2, y + ExpandIconSize / 2,
                ExpandIconColor.R, ExpandIconColor.G, ExpandIconColor.B
            );
        }
        else
        {
            renderer.DrawLine(
                x, y,
                x + ExpandIconSize, y,
                ExpandIconColor.R, ExpandIconColor.G, ExpandIconColor.B
            );
            renderer.DrawLine(
                x + ExpandIconSize / 2, y,
                x + ExpandIconSize / 2, y + ExpandIconSize,
                ExpandIconColor.R, ExpandIconColor.G, ExpandIconColor.B
            );
        }
    }

    #endregion

    #region 公开方法

    public void AddRoot(TreeNode node)
    {
        _roots.Add(node);
        InvalidateMeasure();
    }

    public void RemoveRoot(TreeNode node)
    {
        _roots.Remove(node);
        InvalidateMeasure();
    }

    public void Clear()
    {
        _roots.Clear();
        SelectedNode = null;
        InvalidateMeasure();
    }

    public TreeNode? HitTestNode(float localY)
    {
        var y = 0f;
        return HitTestNodeInternal(_roots, localY, ref y);
    }

    #endregion

    #region 私有方法

    private int CountVisibleNodes()
    {
        return CountVisibleNodesInternal(_roots);
    }

    private static int CountVisibleNodesInternal(IReadOnlyList<TreeNode> nodes)
    {
        var count = 0;

        foreach (var node in nodes)
        {
            count++;

            if (node.IsExpanded)
            {
                count += CountVisibleNodesInternal(node.Children);
            }
        }

        return count;
    }

    private TreeNode? HitTestNodeInternal(IReadOnlyList<TreeNode> nodes, float targetY, ref float currentY)
    {
        foreach (var node in nodes)
        {
            if (currentY + RowHeight > targetY)
            {
                return node;
            }

            currentY += RowHeight;

            if (node.IsExpanded && node.Children.Count > 0)
            {
                var found = HitTestNodeInternal(node.Children, targetY, ref currentY);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    #endregion
}
