using Gnosis.GameUI;
using Gnosis.GameUI.Enums;
using Gnosis.GameUI.ValueObjects;

namespace Gnosis.GameUI;

public sealed class UIElement : IUIElement
{
    #region Properties

    public UIElementType ElementType { get; set; } = UIElementType.Image;

    public float Width { get; set; } = 0;

    public float Height { get; set; } = 0;

    public float X { get; set; } = 0;

    public float Y { get; set; } = 0;

    public float R { get; set; } = 1.0f;

    public float G { get; set; } = 1.0f;

    public float B { get; set; } = 1.0f;

    public float A { get; set; } = 1.0f;

    public AtlasUV AtlasUV { get; set; } = AtlasUV.Full;

    public IStyleBox? StyleBox { get; set; }

    public bool Visible { get; set; } = true;

    #endregion
}
