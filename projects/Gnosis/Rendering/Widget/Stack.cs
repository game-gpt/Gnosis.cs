namespace Gnosis.Rendering.Widget;

// 堆叠定位接口，用于在 Stack 中指定子组件的偏移位置
public interface IStackPositioned
{
    float Left { get; }
    float Top { get; }
}

// 堆叠布局容器，子组件默认从左上角开始堆叠，支持通过 IStackPositioned 接口偏移定位
public sealed class Stack : ContainerWidget
{
    protected override Size MeasureChildren(Size availableSize)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(availableSize);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        return new Size(maxWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var x = contentRect.X;
            var y = contentRect.Y;

            // 如果子组件实现了 IStackPositioned，则应用偏移
            if (child is IStackPositioned positioned)
            {
                x += positioned.Left;
                y += positioned.Top;
            }

            var childRect = new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height);
            child.Arrange(childRect);
        }
    }
}
