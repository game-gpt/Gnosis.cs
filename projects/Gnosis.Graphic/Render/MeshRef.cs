using System.Numerics;
using Gnosis.ECS.Component;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Render;

public struct MeshRef : IComponent
{
    public GpuMesh? Mesh { get; set; }

    public Matrix4x4 Transform { get; set; }

    public bool Visible { get; set; }

    public bool CastShadows { get; set; }
}
