using System.Numerics;
using Gnosis.Physics.NarrowPhase;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Solver;

public sealed class ImpulseSolver
{
    #region 字段

    private float _baumgarteFactor = 0.2f;
    private float _slop = 0.005f;

    #endregion

    #region 属性

    public float BaumgarteFactor
    {
        get => _baumgarteFactor;
        set => _baumgarteFactor = Math.Clamp(value, 0f, 1f);
    }

    public float Slop
    {
        get => _slop;
        set => _slop = Math.Max(0f, value);
    }

    #endregion

    #region 公开方法

    public void Solve(ContactPoint contact, IRigidBody bodyA, IRigidBody bodyB)
    {
        var normal = contact.Normal;
        var penetration = contact.Penetration;

        var restitution = ComputeRestitution(bodyA, bodyB);
        var friction = ComputeFriction(bodyA, bodyB);

        var relativeVelocity = bodyA.Velocity - bodyB.Velocity;
        var velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0f)
        {
            PositionalCorrection(contact, bodyA, bodyB);
            return;
        }

        var invMassA = GetInverseMass(bodyA);
        var invMassB = GetInverseMass(bodyB);
        var invMassSum = invMassA + invMassB;

        if (invMassSum <= 0f)
        {
            return;
        }

        var j = -(1f + restitution) * velAlongNormal / invMassSum;

        var impulse = normal * j;
        ApplyImpulse(bodyA, impulse, invMassA);
        ApplyImpulse(bodyB, -impulse, invMassB);

        SolveFriction(normal, relativeVelocity, invMassSum, friction, bodyA, bodyB);

        PositionalCorrection(contact, bodyA, bodyB);
    }

    #endregion

    #region 私有方法

    private static float GetInverseMass(IRigidBody body)
    {
        if (body.BodyType == RigidBodyType.Static)
        {
            return 0f;
        }

        if (body.IsKinematic)
        {
            return 0f;
        }

        return body.Mass > 0f ? 1f / body.Mass : 0f;
    }

    private static void ApplyImpulse(IRigidBody body, Vector3 impulse, float invMass)
    {
        if (invMass <= 0f)
        {
            return;
        }

        body.Velocity += impulse * invMass;
    }

    private static float ComputeRestitution(IRigidBody bodyA, IRigidBody bodyB)
    {
        var restitutionA = GetRestitution(bodyA);
        var restitutionB = GetRestitution(bodyB);
        return MathF.Max(restitutionA, restitutionB);
    }

    private static float ComputeFriction(IRigidBody bodyA, IRigidBody bodyB)
    {
        var frictionA = GetFriction(bodyA);
        var frictionB = GetFriction(bodyB);
        return MathF.Sqrt(frictionA * frictionB);
    }

    private static float GetRestitution(IRigidBody body)
    {
        if (body.Colliders == null || body.Colliders.Count == 0)
        {
            return 0f;
        }

        var material = body.Colliders[0].Material;
        return material?.Restitution ?? 0f;
    }

    private static float GetFriction(IRigidBody body)
    {
        if (body.Colliders == null || body.Colliders.Count == 0)
        {
            return 0.5f;
        }

        var material = body.Colliders[0].Material;
        return material?.Friction ?? 0.5f;
    }

    private void SolveFriction(
        Vector3 normal,
        Vector3 relativeVelocity,
        float invMassSum,
        float friction,
        IRigidBody bodyA,
        IRigidBody bodyB)
    {
        var tangent = relativeVelocity - normal * Vector3.Dot(relativeVelocity, normal);

        if (tangent.LengthSquared() < 0.0001f)
        {
            return;
        }

        tangent = Vector3.Normalize(tangent);

        var velAlongTangent = Vector3.Dot(relativeVelocity, tangent);
        var jt = -velAlongTangent / invMassSum;

        var invMassA = GetInverseMass(bodyA);
        var invMassB = GetInverseMass(bodyB);

        var jMagnitude = MathF.Abs(Vector3.Dot(relativeVelocity - normal * Vector3.Dot(relativeVelocity, normal), tangent));

        if (MathF.Abs(jt) < jMagnitude * friction)
        {
            var frictionImpulse = tangent * jt;
            ApplyImpulse(bodyA, frictionImpulse, invMassA);
            ApplyImpulse(bodyB, -frictionImpulse, invMassB);
        }
        else
        {
            var frictionImpulse = tangent * (-jMagnitude * friction);
            ApplyImpulse(bodyA, frictionImpulse, invMassA);
            ApplyImpulse(bodyB, -frictionImpulse, invMassB);
        }
    }

    private void PositionalCorrection(ContactPoint contact, IRigidBody bodyA, IRigidBody bodyB)
    {
        var invMassA = GetInverseMass(bodyA);
        var invMassB = GetInverseMass(bodyB);
        var invMassSum = invMassA + invMassB;

        if (invMassSum <= 0f)
        {
            return;
        }

        var correction = MathF.Max(contact.Penetration - _slop, 0f) * _baumgarteFactor / invMassSum;
        var correctionVec = contact.Normal * correction;

        if (invMassA > 0f)
        {
            bodyA.Position -= correctionVec * invMassA;
        }

        if (invMassB > 0f)
        {
            bodyB.Position += correctionVec * invMassB;
        }
    }

    #endregion
}
