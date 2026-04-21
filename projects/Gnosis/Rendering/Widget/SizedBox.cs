namespace Gnosis.Rendering.Widget;

// 固定尺寸盒子，用于强制指定组件的宽高，未指定的维度使用可用空间
public sealed class SizedBox : Widget
{
    #region Properties

    // 盒子宽度，为 null 时使用可用宽度
    public float? BoxWidth { get; set; }

    // 盒子高度，为 null 时使用可用高度
    public float? BoxHeight { get; set; }

    #endregion

    #region Constructors

    public SizedBox() { }

    public SizedBox(float? width = null, float? height = null)
    {
        BoxWidth = width;
        BoxHeight = height;
    }

    #endregion

    protected override Size MeasureOverride(Size availableSize)
    {
        // 未指定的维度使用可用空间
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

    public override void Paint(UiRenderer renderer)
    {
    }
}
