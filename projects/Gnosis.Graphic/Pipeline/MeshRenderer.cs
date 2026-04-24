using System.Numerics;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class MeshRenderer
{
    #region 字段

    private readonly List<RenderItem> _renderItems = [];

    #endregion

    #region 属性

    public IReadOnlyList<RenderItem> RenderItems => _renderItems;

    #endregion

    #region 公开方法

    public RenderItem AddRenderItem(GpuMesh mesh, MaterialInstance material, Matrix4x4 transform)
    {
        var item = new RenderItem(mesh, material, transform);
        _renderItems.Add(item);
        return item;
    }

    public bool RemoveRenderItem(RenderItem item)
    {
        return _renderItems.Remove(item);
    }

    public void Clear()
    {
        _renderItems.Clear();
    }

    public void Render(ICommandTable commandTable, ICamera camera)
    {
        foreach (var item in _renderItems)
        {
            if (!item.Visible)
            {
                continue;
            }

            var gpuMesh = item.Mesh;
            if (gpuMesh.VertexBuffer == null)
            {
                continue;
            }

            commandTable.SetVertexBuffer(gpuMesh.VertexBuffer);

            if (gpuMesh.IndexBuffer != null)
            {
                commandTable.SetIndexBuffer(gpuMesh.IndexBuffer);
                commandTable.DrawIndexed(gpuMesh.IndexCount);
            }
            else
            {
                commandTable.Draw(gpuMesh.VertexCount);
            }
        }
    }

    public void RenderShadow(ICommandTable commandTable)
    {
        foreach (var item in _renderItems)
        {
            if (!item.Visible || !item.CastShadows)
            {
                continue;
            }

            var gpuMesh = item.Mesh;
            if (gpuMesh.VertexBuffer == null)
            {
                continue;
            }

            commandTable.SetVertexBuffer(gpuMesh.VertexBuffer);

            if (gpuMesh.IndexBuffer != null)
            {
                commandTable.SetIndexBuffer(gpuMesh.IndexBuffer);
                commandTable.DrawIndexed(gpuMesh.IndexCount);
            }
            else
            {
                commandTable.Draw(gpuMesh.VertexCount);
            }
        }
    }

    #endregion
}

public sealed class RenderItem
{
    #region 属性

    public GpuMesh Mesh { get; }
    public MaterialInstance Material { get; set; }
    public Matrix4x4 Transform { get; set; }
    public bool Visible { get; set; }
    public bool CastShadows { get; set; }
    public int RenderQueue { get; set; }

    #endregion

    #region 构造函数

    public RenderItem(GpuMesh mesh, MaterialInstance material, Matrix4x4 transform)
    {
        Mesh = mesh;
        Material = material;
        Transform = transform;
        Visible = true;
        CastShadows = true;
        RenderQueue = 0;
    }

    #endregion
}
