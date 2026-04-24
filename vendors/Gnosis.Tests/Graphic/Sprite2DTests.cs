using System.Numerics;
using Gnosis.Graphic.Sprite2D;
using NUnit.Framework;

namespace Gnosis.Tests.Graphic;

[TestFixture]
public class Vertex2DTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var position = new Vector2(1.0f, 2.0f);
        var uv = new Vector2(0.5f, 0.5f);
        var tint = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

        var vertex = new Vertex2D(position, uv, tint);

        Assert.That(vertex.Position, Is.EqualTo(position));
        Assert.That(vertex.Uv, Is.EqualTo(uv));
        Assert.That(vertex.Tint, Is.EqualTo(tint));
    }
}

[TestFixture]
public class RectangleTests
{
    [Test]
    public void Empty_IsEmptyRectangle()
    {
        var empty = Rectangle.Empty;

        Assert.That(empty.IsEmpty, Is.True);
    }

    [Test]
    public void Contains_PointInside_ReturnsTrue()
    {
        var rect = new Rectangle(10, 20, 100, 50);

        Assert.That(rect.Contains(50, 40), Is.True);
    }

    [Test]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var rect = new Rectangle(10, 20, 100, 50);

        Assert.That(rect.Contains(0, 0), Is.False);
    }

    [Test]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        var a = new Rectangle(0, 0, 100, 100);
        var b = new Rectangle(50, 50, 100, 100);

        Assert.That(a.Intersects(b), Is.True);
    }

    [Test]
    public void Intersects_NonOverlappingRectangles_ReturnsFalse()
    {
        var a = new Rectangle(0, 0, 50, 50);
        var b = new Rectangle(100, 100, 50, 50);

        Assert.That(a.Intersects(b), Is.False);
    }

    [Test]
    public void Properties_ReturnCorrectValues()
    {
        var rect = new Rectangle(10, 20, 100, 50);

        Assert.That(rect.Left, Is.EqualTo(10));
        Assert.That(rect.Top, Is.EqualTo(20));
        Assert.That(rect.Right, Is.EqualTo(110));
        Assert.That(rect.Bottom, Is.EqualTo(70));
    }
}

[TestFixture]
public class SpriteFlipTests
{
    [Test]
    public void None_HasNoFlags()
    {
        Assert.That((int)SpriteFlip.None, Is.EqualTo(0));
    }

    [Test]
    public void Horizontal_IsFlag1()
    {
        Assert.That((int)SpriteFlip.Horizontal, Is.EqualTo(1));
    }

    [Test]
    public void Vertical_IsFlag2()
    {
        Assert.That((int)SpriteFlip.Vertical, Is.EqualTo(2));
    }

    [Test]
    public void Both_CanBeCombined()
    {
        var both = SpriteFlip.Horizontal | SpriteFlip.Vertical;

        Assert.That((int)both, Is.EqualTo(3));
    }
}
