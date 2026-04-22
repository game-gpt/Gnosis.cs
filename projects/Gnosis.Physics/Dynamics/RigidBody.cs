using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Dynamics;

public sealed class RigidBody : IRigidBody
{
    #region 字段

    private float[] _velocity = [0f, 0f, 0f];
    private float[] _angularVelocity = [0f, 0f, 0f];
    private float[] _position = [0f, 0f, 0f];
    private float[] _rotation = [0f, 0f, 0f];
    private float[] _accumulatedForce = [0f, 0f, 0f];
    private float[] _accumulatedTorque = [0f, 0f, 0f];

    #endregion

    #region 属性

    public string Name { get; }

    public RigidBodyType BodyType { get; set; } = RigidBodyType.Dynamic;

    public float Mass { get; set; } = 1.0f;

    public float Drag { get; set; }

    public float AngularDrag { get; set; } = 0.05f;

    public bool UseGravity { get; set; } = true;

    public bool IsKinematic { get; set; }

    public float[] Velocity
    {
        get => _velocity;
        set => _velocity = value ?? [0f, 0f, 0f];
    }

    public float[] AngularVelocity
    {
        get => _angularVelocity;
        set => _angularVelocity = value ?? [0f, 0f, 0f];
    }

    public float[] Position
    {
        get => _position;
        set => _position = value ?? [0f, 0f, 0f];
    }

    public float[] Rotation
    {
        get => _rotation;
        set => _rotation = value ?? [0f, 0f, 0f];
    }

    internal IReadOnlyList<ICollider> Colliders => _colliders;

    #endregion

    #region 字段（内部）

    private readonly List<ICollider> _colliders = new();

    #endregion

    #region 构造函数

    public RigidBody(string name, RigidBodyType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        BodyType = type;
    }

    #endregion

    #region IRigidBody 实现

    public void AddForce(float[] force, ForceMode mode = ForceMode.Force)
    {
        if (force is null || force.Length < 3)
        {
            return;
        }

        switch (mode)
        {
            case ForceMode.Force:
                _accumulatedForce[0] += force[0];
                _accumulatedForce[1] += force[1];
                _accumulatedForce[2] += force[2];
                break;
            case ForceMode.Impulse:
                if (Mass > 0f)
                {
                    _velocity[0] += force[0] / Mass;
                    _velocity[1] += force[1] / Mass;
                    _velocity[2] += force[2] / Mass;
                }

                break;
            case ForceMode.VelocityChange:
                _velocity[0] += force[0];
                _velocity[1] += force[1];
                _velocity[2] += force[2];
                break;
            case ForceMode.Acceleration:
                if (Mass > 0f)
                {
                    _accumulatedForce[0] += force[0] * Mass;
                    _accumulatedForce[1] += force[1] * Mass;
                    _accumulatedForce[2] += force[2] * Mass;
                }

                break;
        }
    }

    public void AddTorque(float[] torque, ForceMode mode = ForceMode.Force)
    {
        if (torque is null || torque.Length < 3)
        {
            return;
        }

        _accumulatedTorque[0] += torque[0];
        _accumulatedTorque[1] += torque[1];
        _accumulatedTorque[2] += torque[2];
    }

    public void AddForceAtPosition(float[] force, float[] position, ForceMode mode = ForceMode.Force)
    {
        AddForce(force, mode);

        if (position is null || position.Length < 3)
        {
            return;
        }

        var rx = position[0] - _position[0];
        var ry = position[1] - _position[1];
        var rz = position[2] - _position[2];

        _accumulatedTorque[0] += ry * force[2] - rz * force[1];
        _accumulatedTorque[1] += rz * force[0] - rx * force[2];
        _accumulatedTorque[2] += rx * force[1] - ry * force[0];
    }

    #endregion

    #region 内部方法

    internal void AddCollider(ICollider collider)
    {
        _colliders.Add(collider);
    }

    internal void RemoveCollider(ICollider collider)
    {
        _colliders.Remove(collider);
    }

    internal void Integrate(float delta, float[] gravity)
    {
        if (BodyType != RigidBodyType.Dynamic || IsKinematic)
        {
            _accumulatedForce = [0f, 0f, 0f];
            _accumulatedTorque = [0f, 0f, 0f];
            return;
        }

        if (UseGravity && Mass > 0f)
        {
            _velocity[0] += gravity[0] * delta;
            _velocity[1] += gravity[1] * delta;
            _velocity[2] += gravity[2] * delta;
        }

        if (Mass > 0f)
        {
            _velocity[0] += (_accumulatedForce[0] / Mass) * delta;
            _velocity[1] += (_accumulatedForce[1] / Mass) * delta;
            _velocity[2] += (_accumulatedForce[2] / Mass) * delta;
        }

        var dragFactor = 1f - Drag * delta;

        if (dragFactor < 0f)
        {
            dragFactor = 0f;
        }

        _velocity[0] *= dragFactor;
        _velocity[1] *= dragFactor;
        _velocity[2] *= dragFactor;

        _position[0] += _velocity[0] * delta;
        _position[1] += _velocity[1] * delta;
        _position[2] += _velocity[2] * delta;

        _accumulatedForce = [0f, 0f, 0f];
        _accumulatedTorque = [0f, 0f, 0f];
    }

    #endregion
}
