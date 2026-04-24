using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public sealed class Layer2DManager
{
    #region 属性

    public IReadOnlyList<Layer2D> Layers => _layers;
    public int Count => _layers.Count;

    #endregion

    #region 内部状态

    private readonly List<Layer2D> _layers = [];
    private readonly Dictionary<string, Layer2D> _layersByName = [];

    #endregion

    #region 图层管理

    public Layer2D AddLayer(string name, int order = 0)
    {
        if (_layersByName.ContainsKey(name))
        {
            throw new InvalidOperationException($"图层已存在：{name}");
        }

        var layer = new Layer2D(name, order);
        _layers.Add(layer);
        _layersByName[name] = layer;
        SortLayers();
        return layer;
    }

    public bool RemoveLayer(string name)
    {
        if (!_layersByName.TryGetValue(name, out var layer))
        {
            return false;
        }

        _layers.Remove(layer);
        _layersByName.Remove(name);
        return true;
    }

    public Layer2D? GetLayer(string name)
    {
        return _layersByName.GetValueOrDefault(name);
    }

    public void Clear()
    {
        _layers.Clear();
        _layersByName.Clear();
    }

    #endregion

    #region 排序

    public void SortLayers()
    {
        _layers.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    #endregion

    #region 渲染

    public void Render(SpriteBatch spriteBatch)
    {
        foreach (var layer in _layers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            foreach (var sprite in layer.Sprites)
            {
                var adjustedSprite = sprite with
                {
                    Position = sprite.Position + layer.Offset,
                    Scale = sprite.Scale * layer.Scale,
                    Tint = sprite.Tint * new Vector4(1, 1, 1, layer.Opacity)
                };

                spriteBatch.Draw(
                    adjustedSprite.Texture!,
                    adjustedSprite.Position,
                    adjustedSprite.SourceRectangle,
                    adjustedSprite.Tint,
                    adjustedSprite.Rotation,
                    adjustedSprite.Origin,
                    adjustedSprite.Scale,
                    adjustedSprite.Flip,
                    adjustedSprite.Depth,
                    adjustedSprite.Layer);
            }
        }
    }

    #endregion
}
