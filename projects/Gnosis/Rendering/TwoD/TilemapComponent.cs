using Gnosis.ECS.Core;

namespace Gnosis.Rendering.TwoD;

public struct TilemapComponent : IComponent
{
    public ITilemap? Tilemap { get; set; }
    public float TileSize { get; set; }
}
