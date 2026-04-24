using Gnosis.Graphic.Sprite2D;
using NUnit.Framework;

namespace Gnosis.Tests.Graphic;

[TestFixture]
public class Layer2DManagerTests
{
    [Test]
    public void AddLayer_CreatesLayer_WithCorrectName()
    {
        var manager = new Layer2DManager();

        var layer = manager.AddLayer("Background", 0);

        Assert.That(layer.Name, Is.EqualTo("Background"));
        Assert.That(layer.Order, Is.EqualTo(0));
        Assert.That(manager.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddLayer_ThrowsWhenDuplicate()
    {
        var manager = new Layer2DManager();
        manager.AddLayer("Background", 0);

        Assert.Throws<InvalidOperationException>(() => manager.AddLayer("Background", 1));
    }

    [Test]
    public void RemoveLayer_RemovesByName()
    {
        var manager = new Layer2DManager();
        manager.AddLayer("Background", 0);

        var result = manager.RemoveLayer("Background");

        Assert.That(result, Is.True);
        Assert.That(manager.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveLayer_ReturnsFalseWhenNotFound()
    {
        var manager = new Layer2DManager();

        var result = manager.RemoveLayer("NonExistent");

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetLayer_ReturnsLayerByName()
    {
        var manager = new Layer2DManager();
        var added = manager.AddLayer("UI", 10);

        var found = manager.GetLayer("UI");

        Assert.That(found, Is.SameAs(added));
    }

    [Test]
    public void GetLayer_ReturnsNullWhenNotFound()
    {
        var manager = new Layer2DManager();

        var found = manager.GetLayer("NonExistent");

        Assert.That(found, Is.Null);
    }

    [Test]
    public void Layers_AreSortedByOrder()
    {
        var manager = new Layer2DManager();
        manager.AddLayer("UI", 10);
        manager.AddLayer("Background", 0);
        manager.AddLayer("Characters", 5);

        Assert.That(manager.Layers[0].Name, Is.EqualTo("Background"));
        Assert.That(manager.Layers[1].Name, Is.EqualTo("Characters"));
        Assert.That(manager.Layers[2].Name, Is.EqualTo("UI"));
    }

    [Test]
    public void Clear_RemovesAllLayers()
    {
        var manager = new Layer2DManager();
        manager.AddLayer("A", 0);
        manager.AddLayer("B", 1);

        manager.Clear();

        Assert.That(manager.Count, Is.EqualTo(0));
    }
}

[TestFixture]
public class Layer2DTests
{
    [Test]
    public void AddSprite_IncreasesSpriteCount()
    {
        var layer = new Layer2D("Test", 0);
        var sprite = new Sprite { Position = new System.Numerics.Vector2(10, 20) };

        layer.AddSprite(sprite);

        Assert.That(layer.Sprites.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveSprite_DecreasesSpriteCount()
    {
        var layer = new Layer2D("Test", 0);
        var sprite = new Sprite { Position = new System.Numerics.Vector2(10, 20) };
        layer.AddSprite(sprite);

        var result = layer.RemoveSprite(sprite);

        Assert.That(result, Is.True);
        Assert.That(layer.Sprites.Count, Is.EqualTo(0));
    }

    [Test]
    public void Clear_RemovesAllSprites()
    {
        var layer = new Layer2D("Test", 0);
        layer.AddSprite(new Sprite());
        layer.AddSprite(new Sprite());

        layer.Clear();

        Assert.That(layer.Sprites.Count, Is.EqualTo(0));
    }

    [Test]
    public void ParallaxFactor_DefaultsToOne()
    {
        var layer = new Layer2D("Test", 0);

        Assert.That(layer.ParallaxFactor, Is.EqualTo(1.0f));
    }

    [Test]
    public void UseParallax_DefaultsToTrue()
    {
        var layer = new Layer2D("Test", 0);

        Assert.That(layer.UseParallax, Is.True);
    }

    [Test]
    public void Visible_DefaultsToTrue()
    {
        var layer = new Layer2D("Test", 0);

        Assert.That(layer.Visible, Is.True);
    }

    [Test]
    public void Opacity_DefaultsToOne()
    {
        var layer = new Layer2D("Test", 0);

        Assert.That(layer.Opacity, Is.EqualTo(1.0f));
    }
}
