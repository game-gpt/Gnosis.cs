using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class SizedBox : WidgetElement
{
    #region 属性

    public float? BoxWidth { get; set; }

    public float? BoxHeight { get; set; }

    #endregion

    #region 构造函数

    public SizedBox() { }

    public SizedBox(float? width = null, float? height = null)
    {
        BoxWidth = width;
        BoxHeight = height;
    }

    #endregion

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = BoxWidth ?? availableSize.Width;
        var h = BoxHeight ?? availableSize.Height;

        return new Size(
            Math.Min(w, availableSize.Width),
            Math.Min(h, availableSize.Height)
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(IWidgetRenderer renderer)
    {
    }
}
