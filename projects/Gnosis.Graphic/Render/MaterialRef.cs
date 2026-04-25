using Gnosis.ECS.Component;
using Gnosis.Graphic.Material;

namespace Gnosis.Graphic.Render;

public struct MaterialRef : IComponent
{
    public MaterialInstance? Material { get; set; }

    public int RenderQueue { get; set; }
}
