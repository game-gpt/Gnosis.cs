using Gnosis.ECS.Component;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Render;

public struct CameraRef : IComponent
{
    public ICamera? Camera { get; set; }

    public bool IsMainCamera { get; set; }
}
