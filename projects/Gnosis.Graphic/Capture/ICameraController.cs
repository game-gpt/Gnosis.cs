using Gnosis.Core.Math;

namespace Gnosis.Graphic.Capture;

public interface ICameraController
{
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }
    float MoveSpeed { get; set; }
    float RotateSpeed { get; set; }
    bool IsEnabled { get; set; }
    void Update(float delta);
}
