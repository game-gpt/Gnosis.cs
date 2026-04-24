using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public record Sprite
{
    public IResource? Texture { get; init; }
    public Vector2 Position { get; init; }
    public Vector2 Size { get; init; }
    public Vector2 Scale { get; init; } = Vector2.One;
    public float Rotation { get; init; }
    public Vector4 Tint { get; init; } = Vector4.One;
    public Rectangle SourceRectangle { get; init; }
    public float Depth { get; init; }
    public SpriteFlip Flip { get; init; } = SpriteFlip.None;
    public Vector2 Origin { get; init; }
    public int Layer { get; init; }
}
