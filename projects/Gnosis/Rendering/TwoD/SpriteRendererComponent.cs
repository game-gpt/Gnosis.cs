using Gnosis.ECS.Core;

namespace Gnosis.Rendering.TwoD;

public struct SpriteRendererComponent : IComponent
{
    public ISpriteRenderer? Renderer { get; set; }
    public string? SpritePath { get; set; }
    public int SortingOrder { get; set; }
}
