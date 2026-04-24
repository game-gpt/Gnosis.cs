using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public sealed class SpriteBatch : IDisposable
{
    #region 常量

    private const int InitialBatchSize = 256;
    private const int MaxBatchSize = 8192;
    private const int VerticesPerSprite = 4;
    private const int IndicesPerSprite = 6;

    #endregion

    #region 属性

    public int BatchCount => _batchItems.Count;

    #endregion

    #region 内部状态

    private readonly IDevice _device;
    private readonly List<SpriteBatchItem> _batchItems;
    private readonly SpriteBatchItemComparer _comparer;
    private readonly List<Vertex2D> _vertices;
    private readonly List<uint> _indices;
    private bool _isBatching;
    private BlendMode _currentBlendMode;
    private SpriteSortMode _sortMode;
    private Matrix4x4 _transformMatrix;
    private IPipelineState? _pipelineState;
    private IResource? _vertexBuffer;
    private IResource? _indexBuffer;
    private bool _isDisposed;

    #endregion

    #region 构造函数

    public SpriteBatch(IDevice device)
    {
        _device = device;
        _batchItems = new List<SpriteBatchItem>(InitialBatchSize);
        _comparer = new SpriteBatchItemComparer();
        _vertices = new List<Vertex2D>(MaxBatchSize * VerticesPerSprite);
        _indices = new List<uint>(MaxBatchSize * IndicesPerSprite);
        _transformMatrix = Matrix4x4.Identity;
    }

    #endregion

    #region 初始化

    public void SetPipelineState(IPipelineState pipelineState)
    {
        _pipelineState = pipelineState;
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

    #region 渲染

    public void Render(ICommandTable commandTable)
    {
        if (_batchItems.Count == 0)
        {
            return;
        }

        if (_sortMode != SpriteSortMode.Immediate)
        {
            SortBatchItems();
        }

        BuildVertexData();

        if (_vertices.Count == 0)
        {
            return;
        }

        UploadBuffers();

        if (_pipelineState != null)
        {
            commandTable.SetPipelineState(_pipelineState);
        }

        if (_vertexBuffer != null)
        {
            commandTable.SetVertexBuffer(_vertexBuffer);
        }

        if (_indexBuffer != null)
        {
            commandTable.SetIndexBuffer(_indexBuffer);
            commandTable.DrawIndexed((uint)_indices.Count);
        }
        else
        {
            commandTable.Draw((uint)_vertices.Count);
        }

        _batchItems.Clear();
        _vertices.Clear();
        _indices.Clear();
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

        BuildVertexData();

        if (_vertices.Count == 0)
        {
            return;
        }

        UploadBuffers();

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        if (_pipelineState != null)
        {
            commandTable.SetPipelineState(_pipelineState);
        }

        if (_vertexBuffer != null)
        {
            commandTable.SetVertexBuffer(_vertexBuffer);
        }

        if (_indexBuffer != null)
        {
            commandTable.SetIndexBuffer(_indexBuffer);
            commandTable.DrawIndexed((uint)_indices.Count);
        }
        else
        {
            commandTable.Draw((uint)_vertices.Count);
        }

        commandTable.End();
        _device.Submit(commandTable);
        commandTable.Dispose();

        _batchItems.Clear();
        _vertices.Clear();
        _indices.Clear();
    }

    private void SortBatchItems()
    {
        _comparer.SortMode = _sortMode;
        _batchItems.Sort(_comparer);
    }

    private void BuildVertexData()
    {
        _vertices.Clear();
        _indices.Clear();

        foreach (var item in _batchItems)
        {
            if (item.Texture == null)
            {
                continue;
            }

            var vertexStart = (uint)_vertices.Count;

            BuildSpriteVertices(item);

            _indices.Add(vertexStart);
            _indices.Add(vertexStart + 1);
            _indices.Add(vertexStart + 2);
            _indices.Add(vertexStart);
            _indices.Add(vertexStart + 2);
            _indices.Add(vertexStart + 3);
        }
    }

    private void BuildSpriteVertices(SpriteBatchItem item)
    {
        var position = item.Position;
        var scale = item.Scale;
        var origin = item.Origin;
        var rotation = item.Rotation;
        var tint = item.Tint;
        var flip = item.Flip;

        float width, height;

        if (item.UseDestinationSize)
        {
            width = item.DestinationSize.X;
            height = item.DestinationSize.Y;
        }
        else
        {
            width = scale.X;
            height = scale.Y;
        }

        var topLeft = -origin;
        var topRight = new Vector2(width - origin.X, -origin.Y);
        var bottomLeft = new Vector2(-origin.X, height - origin.Y);
        var bottomRight = new Vector2(width - origin.X, height - origin.Y);

        if (rotation != 0.0f)
        {
            var cos = MathF.Cos(rotation);
            var sin = MathF.Sin(rotation);

            topLeft = RotatePoint(topLeft, cos, sin);
            topRight = RotatePoint(topRight, cos, sin);
            bottomLeft = RotatePoint(bottomLeft, cos, sin);
            bottomRight = RotatePoint(bottomRight, cos, sin);
        }

        topLeft = TransformVertex(topLeft + position);
        topRight = TransformVertex(topRight + position);
        bottomLeft = TransformVertex(bottomLeft + position);
        bottomRight = TransformVertex(bottomRight + position);

        var uvTopLeft = new Vector2(0.0f, 0.0f);
        var uvTopRight = new Vector2(1.0f, 0.0f);
        var uvBottomLeft = new Vector2(0.0f, 1.0f);
        var uvBottomRight = new Vector2(1.0f, 1.0f);

        if (!item.SourceRectangle.IsEmpty)
        {
            uvTopLeft = new Vector2(item.SourceRectangle.X, item.SourceRectangle.Y);
            uvTopRight = new Vector2(item.SourceRectangle.Right, item.SourceRectangle.Y);
            uvBottomLeft = new Vector2(item.SourceRectangle.X, item.SourceRectangle.Bottom);
            uvBottomRight = new Vector2(item.SourceRectangle.Right, item.SourceRectangle.Bottom);
        }

        if ((flip & SpriteFlip.Horizontal) != 0)
        {
            (uvTopLeft.X, uvTopRight.X) = (uvTopRight.X, uvTopLeft.X);
            (uvBottomLeft.X, uvBottomRight.X) = (uvBottomRight.X, uvBottomLeft.X);
        }

        if ((flip & SpriteFlip.Vertical) != 0)
        {
            (uvTopLeft.Y, uvBottomLeft.Y) = (uvBottomLeft.Y, uvTopLeft.Y);
            (uvTopRight.Y, uvBottomRight.Y) = (uvBottomRight.Y, uvTopRight.Y);
        }

        _vertices.Add(new Vertex2D(topLeft, uvTopLeft, tint));
        _vertices.Add(new Vertex2D(topRight, uvTopRight, tint));
        _vertices.Add(new Vertex2D(bottomRight, uvBottomRight, tint));
        _vertices.Add(new Vertex2D(bottomLeft, uvBottomLeft, tint));
    }

    private Vector2 RotatePoint(Vector2 point, float cos, float sin)
    {
        return new Vector2(
            point.X * cos - point.Y * sin,
            point.X * sin + point.Y * cos
        );
    }

    private Vector2 TransformVertex(Vector2 vertex)
    {
        if (_transformMatrix == Matrix4x4.Identity)
        {
            return vertex;
        }

        var v4 = Vector4.Transform(new Vector4(vertex, 0.0f, 1.0f), _transformMatrix);
        return new Vector2(v4.X, v4.Y);
    }

    private void UploadBuffers()
    {
        var vertexData = MemoryMarshal.AsBytes<Vertex2D>(_vertices.ToArray());
        var vertexSize = (ulong)vertexData.Length;

        _vertexBuffer?.Dispose();
        _vertexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = vertexSize,
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = vertexData.ToArray()
        });

        var indexData = MemoryMarshal.AsBytes(_indices.ToArray());
        var indexSize = (ulong)indexData.Length;

        _indexBuffer?.Dispose();
        _indexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = indexSize,
            Usage = BufferUsage.IndexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = indexData.ToArray()
        });
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

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
