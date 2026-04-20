namespace Gnosis.GameUI;

public interface IUIElement
{
    UIElementType ElementType { get; }
    float Width { get; set; }
    float Height { get; set; }
    float X { get; set; }
    float Y { get; set; }
    float R { get; set; }
    float G { get; set; }
    float B { get; set; }
    float A { get; set; }
    AtlasUV AtlasUV { get; set; }
    IStyleBox? StyleBox { get; set; }
    bool Visible { get; set; }
}
