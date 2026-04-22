using Gnosis.ECS.Core;

namespace Gnosis.Graphic.Capture;

public struct CameraControllerComponent : IComponent
{
    public ICameraController? Controller { get; set; }
    public float MoveSpeed { get; set; }
}
