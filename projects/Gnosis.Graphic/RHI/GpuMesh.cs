using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

public sealed class GpuMesh : IMesh
{
    #region 字段

    private readonly IDevice _device;
    private IResource? _vertexBuffer;
    private IResource? _indexBuffer;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public uint VertexCount { get; private set; }
    public uint IndexCount { get; private set; }
    public uint SubMeshCount { get; private set; }
    public IResource? VertexBuffer => _vertexBuffer;
    public IResource? IndexBuffer => _indexBuffer;
    public uint VertexStride { get; private set; }
    public BoundingBox Bounds { get; private set; }

    #endregion

    #region 构造函数

    public GpuMesh(IDevice device, string name)
    {
        _device = device;
        Name = name;
        VertexStride = (uint)sizeof(Vertex3D);
        Bounds = BoundingBox.Empty;
    }

    #endregion

    #region 公开方法

    public void UploadVertices(ReadOnlySpan<Vertex3D> vertices)
    {
        VertexCount = (uint)vertices.Length;

        if (vertices.IsEmpty)
        {
            return;
        }

        var dataSize = (uint)(vertices.Length * sizeof(Vertex3D));
        var data = MemoryMarshal.AsBytes(vertices);

        _vertexBuffer?.Dispose();
        _vertexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = dataSize,
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = data.ToArray()
        });

        UpdateBounds(vertices);
    }

    public void UploadIndices(ReadOnlySpan<uint> indices)
    {
        IndexCount = (uint)indices.Length;

        if (indices.IsEmpty)
        {
            return;
        }

        var dataSize = (uint)(indices.Length * sizeof(uint));
        var data = MemoryMarshal.AsBytes(indices);

        _indexBuffer?.Dispose();
        _indexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = dataSize,
            Usage = BufferUsage.IndexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = data.ToArray()
        });
    }

    public void UploadIndices(ReadOnlySpan<ushort> indices)
    {
        IndexCount = (uint)indices.Length;

        if (indices.IsEmpty)
        {
            return;
        }

        var dataSize = (uint)(indices.Length * sizeof(ushort));
        var data = MemoryMarshal.AsBytes(indices);

        _indexBuffer?.Dispose();
        _indexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = dataSize,
            Usage = BufferUsage.IndexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = data.ToArray()
        });
    }

    public void SetSubMeshCount(uint count)
    {
        SubMeshCount = count;
    }

    #endregion

    #region 私有方法

    private void UpdateBounds(ReadOnlySpan<Vertex3D> vertices)
    {
        if (vertices.IsEmpty)
        {
            Bounds = BoundingBox.Empty;
            return;
        }

        var min = vertices[0].Position;
        var max = vertices[0].Position;

        for (var i = 1; i < vertices.Length; i++)
        {
            var pos = vertices[i].Position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        Bounds = new BoundingBox(min, max);
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

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 Uv;

    public Vertex3D(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        Uv = uv;
    }
}
