using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public sealed class SpriteBatch
{
    #region 常量

    private const int InitialBatchSize = 256;
    private const int MaxBatchSize = 8192;

    #endregion

    #region 内部状态

    private readonly IDevice _device;
    private readonly List<SpriteBatchItem> _batchItems;
    private readonly SpriteBatchItemComparer _comparer;
    private bool _isBatching;
    private IResource? _currentTexture;
    private BlendMode _currentBlendMode;
    private SpriteSortMode _sortMode;
    private Matrix4x4 _transformMatrix;

    #endregion

    #region 构造函数

    public SpriteBatch(IDevice device)
    {
        _device = device;
        _batchItems = new List<SpriteBatchItem>(InitialBatchSize);
        _comparer = new SpriteBatchItemComparer();
        _transformMatrix = Matrix4x4.Identity;
    }

    #endregion

    #region 批次控制

    public void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendMode blendMode = BlendMode.Alpha, in Matrix4x4 transformMatrix = default)
    {
        if (_isBatching)
        {
            throw new InvalidOperationException("SpriteBatch 已在批处理中，请先调用 End");
        }

        _isBatching = true;
        _sortMode = sortMode;
        _currentBlendMode = blendMode;
        _transformMatrix = transformMatrix == default ? Matrix4x4.Identity : transformMatrix;
        _currentTexture = null;
    }

    public void End()
    {
        if (!_isBatching)
        {
            throw new InvalidOperationException("SpriteBatch 未在批处理中，请先调用 Begin");
        }

        FlushBatch();
        _isBatching = false;
    }

    #endregion

    #region 绘制方法

    public void Draw(IResource texture, Vector2 position, in Vector4 tint)
    {
        Draw(texture, position, Rectangle.Empty, tint, 0.0f, Vector2.Zero, Vector2.One, SpriteFlip.None, 0.0f, 0);
    }

    public void Draw(IResource texture, Vector2 position, Rectangle sourceRectangle, in Vector4 tint)
    {
        Draw(texture, position, sourceRectangle, tint, 0.0f, Vector2.Zero, Vector2.One, SpriteFlip.None, 0.0f, 0);
    }

    public void Draw(IResource texture, Vector2 position, Rectangle sourceRectangle, in Vector4 tint, float rotation, Vector2 origin, Vector2 scale, SpriteFlip flip = SpriteFlip.None, float depth = 0.0f, int layer = 0)
    {
        AssertBatching();

        var item = new SpriteBatchItem
        {
            Texture = texture,
            Position = position,
            SourceRectangle = sourceRectangle,
            Tint = tint,
            Rotation = rotation,
            Origin = origin,
            Scale = scale,
            Flip = flip,
            Depth = depth,
            Layer = layer,
            BlendMode = _currentBlendMode
        };

        _batchItems.Add(item);

        if (_sortMode == SpriteSortMode.Immediate)
        {
            FlushBatch();
        }
    }

    public void Draw(IResource texture, Rectangle destinationRectangle, in Vector4 tint)
    {
        AssertBatching();

        var item = new SpriteBatchItem
        {
            Texture = texture,
            Position = new Vector2(destinationRectangle.X, destinationRectangle.Y),
            SourceRectangle = Rectangle.Empty,
            DestinationSize = new Vector2(destinationRectangle.Width, destinationRectangle.Height),
            Tint = tint,
            Rotation = 0.0f,
            Origin = Vector2.Zero,
            Scale = Vector2.One,
            Flip = SpriteFlip.None,
            Depth = 0.0f,
            Layer = 0,
            BlendMode = _currentBlendMode,
            UseDestinationSize = true
        };

        _batchItems.Add(item);

        if (_sortMode == SpriteSortMode.Immediate)
        {
            FlushBatch();
        }
    }

    #endregion

    #region 私有方法

    private void AssertBatching()
    {
        if (!_isBatching)
        {
            throw new InvalidOperationException("SpriteBatch 未在批处理中，请先调用 Begin");
        }
    }

    private void FlushBatch()
    {
        if (_batchItems.Count == 0)
        {
            return;
        }

        if (_sortMode != SpriteSortMode.Immediate)
        {
            SortBatchItems();
        }

        RenderBatchItems();

        _batchItems.Clear();
        _currentTexture = null;
    }

    private void SortBatchItems()
    {
        _comparer.SortMode = _sortMode;
        _batchItems.Sort(_comparer);
    }

    private void RenderBatchItems()
    {
        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        foreach (var item in _batchItems)
        {
            if (_currentTexture != item.Texture)
            {
                if (_currentTexture != null)
                {
                    FlushCurrentBatch(commandTable);
                }

                _currentTexture = item.Texture;
            }

            RenderSprite(commandTable, item);
        }

        FlushCurrentBatch(commandTable);
        commandTable.End();
        _device.Submit(commandTable);
    }

    private void FlushCurrentBatch(ICommandTable commandTable)
    {
    }

    private void RenderSprite(ICommandTable commandTable, SpriteBatchItem item)
    {
    }

    #endregion

    #region 内部类型

    private struct SpriteBatchItem
    {
        public IResource Texture;
        public Vector2 Position;
        public Rectangle SourceRectangle;
        public Vector2 DestinationSize;
        public Vector4 Tint;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 Scale;
        public SpriteFlip Flip;
        public float Depth;
        public int Layer;
        public BlendMode BlendMode;
        public bool UseDestinationSize;
    }

    private class SpriteBatchItemComparer : IComparer<SpriteBatchItem>
    {
        public SpriteSortMode SortMode { get; set; } = SpriteSortMode.Deferred;

        public int Compare(SpriteBatchItem x, SpriteBatchItem y)
        {
            return SortMode switch
            {
                SpriteSortMode.Texture => CompareTexture(x, y),
                SpriteSortMode.FrontToBack => x.Depth.CompareTo(y.Depth),
                SpriteSortMode.BackToFront => y.Depth.CompareTo(x.Depth),
                _ => 0
            };
        }

        private static int CompareTexture(SpriteBatchItem x, SpriteBatchItem y)
        {
            return x.Texture?.Id.CompareTo(y.Texture?.Id ?? 0) ?? 0;
        }
    }

    #endregion
}
