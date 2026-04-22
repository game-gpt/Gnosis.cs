using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Layout;

public sealed class Wrap : ContainerElement
{
    #region 属性

    public WrapDirection Direction { get; set; } = WrapDirection.Horizontal;

    public float Spacing { get; set; } = 0;

    public float RunSpacing { get; set; } = 0;

    #endregion

    #region 字段

    private readonly List<List<WidgetElement>> _runs = [];

    #endregion

    protected override Size MeasureChildren(Size availableSize)
    {
        _runs.Clear();
        var currentRun = new List<WidgetElement>();
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

            var childMain = Direction == WrapDirection.Horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childCross = Direction == WrapDirection.Horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;
            var availableMain = Direction == WrapDirection.Horizontal ? availableSize.Width : availableSize.Height;

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

            if (!firstInRun)
            {
                mainAxisUsed += Spacing;
            }
            firstInRun = false;

            mainAxisUsed += childMain;
            crossAxisMax = Math.Max(crossAxisMax, childCross);
            currentRun.Add(child);
        }

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

                if (!firstInRun)
                {
                    mainOffset += Spacing;
                }
                firstInRun = false;

                var x = Direction == WrapDirection.Horizontal ? mainOffset : crossOffset;
                var y = Direction == WrapDirection.Horizontal ? crossOffset : mainOffset;

                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                mainOffset += childMain;
                runCrossMax = Math.Max(runCrossMax, childCross);
            }

            crossOffset += runCrossMax + RunSpacing;
        }
    }
}
