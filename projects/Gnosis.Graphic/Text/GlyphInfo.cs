using System.Numerics;
using Gnosis.Graphic.Sprite2D;

namespace Gnosis.Graphic.Text;

public record GlyphInfo
{
    public uint CodePoint { get; init; }
    public float XAdvance { get; init; }
    public Vector2 Offset { get; init; }
    public Vector2 Size { get; init; }
    public Rectangle UV { get; init; }
    public int Page { get; init; }
}
