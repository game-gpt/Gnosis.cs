namespace Gnosis.Rendering.TwoD;

public interface ISprite
{
    string Name { get; }
    string TexturePath { get; }
    float[] UvRect { get; }
    float Width { get; }
    float Height { get; }
    float PivotX { get; }
    float PivotY { get; }
    bool IsNineSlice { get; }
    float[] Border { get; }
}
