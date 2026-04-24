using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public sealed class Layer2D
{
    #region 属性

    public string Name { get; }
    public int Order { get; set; }
    public bool Visible { get; set; } = true;
    public float Opacity { get; set; } = 1.0f;
    public Vector2 Offset { get; set; }
    public float Scale { get; set; } = 1.0f;
    public IReadOnlyList<Sprite> Sprites => _sprites;

    #endregion

    #region 内部状态

    private readonly List<Sprite> _sprites = [];

    #endregion

    #region 构造函数

    public Layer2D(string name, int order = 0)
    {
        Name = name;
        Order = order;
    }

    #endregion

    #region 精灵管理

    public void AddSprite(Sprite sprite)
    {
        _sprites.Add(sprite);
    }

    public bool RemoveSprite(Sprite sprite)
    {
        return _sprites.Remove(sprite);
    }

    public void Clear()
    {
        _sprites.Clear();
    }

    #endregion
}
