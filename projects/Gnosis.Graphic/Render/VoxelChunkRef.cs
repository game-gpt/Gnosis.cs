using Gnosis.ECS.Component;
using Gnosis.Graphic.Pipeline;

namespace Gnosis.Graphic.Render;

public struct VoxelChunkRef : IComponent
{
    public VoxelChunk? Chunk { get; set; }

    public bool Visible { get; set; }
}
