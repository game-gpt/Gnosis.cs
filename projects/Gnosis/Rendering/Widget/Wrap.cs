namespace Gnosis.Rendering.Widget;

// 换行方向枚举
public enum WrapDirection
{
    Horizontal,
    Vertical
}

// 换行布局容器，子组件沿主轴排列，超出可用空间时自动换行
public sealed class Wrap : ContainerWidget
{
    #region Properties

    // 主轴排列方向
    public WrapDirection Direction { get; set; } = WrapDirection.Horizontal;

    // 同一行内子组件之间的间距
    public float Spacing { get; set; } = 0;

    // 行与行之间的间距
    public float RunSpacing { get; set; } = 0;

    #endregion

    #region Fields

    // 存储每一行的子组件列表
    private readonly List<List<Widget>> _runs = [];

    #endregion

    protected override Size MeasureChildren(Size availableSize)
    {
        _runs.Clear();
        var currentRun = new List<Widget>();
        float mainAxisUsed = 0;
        float crossAxisMax = 0;
        float totalCross = 0;
        float totalMain = 0;
        var firstInRun = true;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(availableSize);

            // 根据方向获取子组件在主轴和交叉轴上的尺寸
            var childMain = Direction == WrapDirection.Horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childCross = Direction == WrapDirection.Horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;
            var availableMain = Direction == WrapDirection.Horizontal ? availableSize.Width : availableSize.Height;

            // 判断是否需要换行：当前行已有子组件且加入后超出可用空间
            if (!firstInRun && mainAxisUsed + Spacing + childMain > availableMain && currentRun.Count > 0)
            {
                totalCross += crossAxisMax + RunSpacing;
                totalMain = Math.Max(totalMain, mainAxisUsed);
                _runs.Add(currentRun);
                currentRun = [];
                mainAxisUsed = 0;
                crossAxisMax = 0;
                firstInRun = true;
            }

            // 非首个子组件时添加间距
            if (!firstInRun)
            {
                mainAxisUsed += Spacing;
            }
            firstInRun = false;

            mainAxisUsed += childMain;
            crossAxisMax = Math.Max(crossAxisMax, childCross);
            currentRun.Add(child);
        }

        // 处理最后一行
        if (currentRun.Count > 0)
        {
            totalCross += crossAxisMax;
            totalMain = Math.Max(totalMain, mainAxisUsed);
            _runs.Add(currentRun);
        }

        return Direction == WrapDirection.Horizontal
            ? new Size(totalMain, totalCross)
            : new Size(totalCross, totalMain);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        // 交叉轴偏移量，从内容区域起始位置开始
        var crossOffset = Direction == WrapDirection.Horizontal ? contentRect.Y : contentRect.X;

        foreach (var run in _runs)
        {
            var mainOffset = Direction == WrapDirection.Horizontal ? contentRect.X : contentRect.Y;
            float runCrossMax = 0;
            var firstInRun = true;

            foreach (var child in run)
            {
                var childMain = Direction == WrapDirection.Horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var childCross = Direction == WrapDirection.Horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;

                // 非首个子组件时添加间距
                if (!firstInRun)
                {
                    mainOffset += Spacing;
                }
                firstInRun = false;

                // 根据方向计算子组件位置
                var x = Direction == WrapDirection.Horizontal ? mainOffset : crossOffset;
                var y = Direction == WrapDirection.Horizontal ? crossOffset : mainOffset;

                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                mainOffset += childMain;
                runCrossMax = Math.Max(runCrossMax, childCross);
            }

            // 累加行间距
            crossOffset += runCrossMax + RunSpacing;
        }
    }
}
