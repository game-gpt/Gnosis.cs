using Gnosis.Core.Math;

namespace Gnosis.Physics.Dynamics;

public interface IRigidBody
{
    string Name { get; }
    RigidBodyType BodyType { get; set; }
    float Mass { get; set; }
    float Drag { get; set; }
    float AngularDrag { get; set; }
    bool UseGravity { get; set; }
    bool IsKinematic { get; set; }
    Vector3 Velocity { get; set; }
    Vector3 AngularVelocity { get; set; }
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }
    void AddForce(Vector3 force, ForceMode mode = ForceMode.Force);
    void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force);
    void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force);
}

public enum ForceMode
{
    Force = 0,
    Impulse = 1,
    VelocityChange = 2,
    Acceleration = 3
}
