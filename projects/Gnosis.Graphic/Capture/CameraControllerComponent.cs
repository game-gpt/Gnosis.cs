using Gnosis.ECS.Core;

namespace Gnosis.Camera;

public struct CameraControllerComponent : IComponent
{
    public ICameraController? Controller { get; set; }
    public float MoveSpeed { get; set; }
}
