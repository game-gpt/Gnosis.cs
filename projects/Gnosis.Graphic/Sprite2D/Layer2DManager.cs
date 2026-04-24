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
                if (sprite.Texture == null)
                {
                    continue;
                }

                var adjustedPosition = layer.UseParallax
                    ? sprite.Position + layer.Offset
                    : sprite.Position + layer.Offset;

                var adjustedScale = sprite.Scale * layer.Scale;
                var adjustedTint = sprite.Tint * new Vector4(1, 1, 1, layer.Opacity);

                spriteBatch.Draw(
                    sprite.Texture,
                    adjustedPosition,
                    sprite.SourceRectangle,
                    adjustedTint,
                    sprite.Rotation,
                    sprite.Origin,
                    adjustedScale,
                    sprite.Flip,
                    sprite.Depth,
                    sprite.Layer);
            }
        }
    }

    /// <summary>
    /// 使用相机偏移渲染所有可见图层，支持视差滚动
    /// </summary>
    /// <param name="spriteBatch">精灵批处理器</param>
    /// <param name="cameraOffset">相机世界偏移</param>
    public void Render(SpriteBatch spriteBatch, Vector2 cameraOffset)
    {
        foreach (var layer in _layers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            var parallaxOffset = layer.UseParallax
                ? cameraOffset * layer.ParallaxFactor
                : Vector2.Zero;

            foreach (var sprite in layer.Sprites)
            {
                if (sprite.Texture == null)
                {
                    continue;
                }

                var adjustedPosition = sprite.Position + layer.Offset - parallaxOffset;
                var adjustedScale = sprite.Scale * layer.Scale;
                var adjustedTint = sprite.Tint * new Vector4(1, 1, 1, layer.Opacity);

                spriteBatch.Draw(
                    sprite.Texture,
                    adjustedPosition,
                    sprite.SourceRectangle,
                    adjustedTint,
                    sprite.Rotation,
                    sprite.Origin,
                    adjustedScale,
                    sprite.Flip,
                    sprite.Depth,
                    sprite.Layer);
            }
        }
    }

    #endregion
}
