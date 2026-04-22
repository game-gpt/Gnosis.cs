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
    float[] Velocity { get; set; }
    float[] AngularVelocity { get; set; }
    float[] Position { get; set; }
    float[] Rotation { get; set; }
    void AddForce(float[] force, ForceMode mode = ForceMode.Force);
    void AddTorque(float[] torque, ForceMode mode = ForceMode.Force);
    void AddForceAtPosition(float[] force, float[] position, ForceMode mode = ForceMode.Force);
}

public enum ForceMode
{
    Force = 0,
    Impulse = 1,
    VelocityChange = 2,
    Acceleration = 3
}
