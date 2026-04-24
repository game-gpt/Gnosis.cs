using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Dynamics;

public sealed class RigidBody : IRigidBody
{
    #region 字段

    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _accumulatedForce;
    private Vector3 _accumulatedTorque;

    #endregion

    #region 属性

    public string Name { get; }

    public RigidBodyType BodyType { get; set; } = RigidBodyType.Dynamic;

    public float Mass { get; set; } = 1.0f;

    public float Drag { get; set; }

    public float AngularDrag { get; set; } = 0.05f;

    public bool UseGravity { get; set; } = true;

    public bool IsKinematic { get; set; }

    public Vector3 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public Vector3 AngularVelocity
    {
        get => _angularVelocity;
        set => _angularVelocity = value;
    }

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = value;
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

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        switch (mode)
        {
            case ForceMode.Force:
                _accumulatedForce += force;
                break;
            case ForceMode.Impulse:
                if (Mass > 0f)
                {
                    _velocity += force / Mass;
                }
                break;
            case ForceMode.VelocityChange:
                _velocity += force;
                break;
            case ForceMode.Acceleration:
                if (Mass > 0f)
                {
                    _accumulatedForce += force * Mass;
                }
                break;
        }
    }

    public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
    {
        _accumulatedTorque += torque;
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
    {
        AddForce(force, mode);

        var r = position - _position;
        _accumulatedTorque += Vector3.Cross(r, force);
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

    internal void Integrate(float delta, Vector3 gravity)
    {
        if (BodyType != RigidBodyType.Dynamic || IsKinematic)
        {
            _accumulatedForce = Vector3.Zero;
            _accumulatedTorque = Vector3.Zero;
            return;
        }

        if (UseGravity && Mass > 0f)
        {
            _velocity += gravity * delta;
        }

        if (Mass > 0f)
        {
            _velocity += (_accumulatedForce / Mass) * delta;
        }

        var dragFactor = 1f - Drag * delta;

        if (dragFactor < 0f)
        {
            dragFactor = 0f;
        }

        _velocity *= dragFactor;

        _position += _velocity * delta;

        _accumulatedForce = Vector3.Zero;
        _accumulatedTorque = Vector3.Zero;
    }

    #endregion
}
