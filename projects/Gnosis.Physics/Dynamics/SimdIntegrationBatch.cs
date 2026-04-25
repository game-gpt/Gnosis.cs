using System.Numerics;
using Gnosis.ECS.Simd;

namespace Gnosis.Physics.Dynamics;

public sealed class SimdIntegrationBatch
{
    #region 字段

    private float[] _positionsX = [];
    private float[] _positionsY = [];
    private float[] _positionsZ = [];
    private float[] _velocitiesX = [];
    private float[] _velocitiesY = [];
    private float[] _velocitiesZ = [];
    private float[] _forcesX = [];
    private float[] _forcesY = [];
    private float[] _forcesZ = [];
    private float[] _masses = [];
    private float[] _drags = [];
    private bool[] _useGravity = [];
    private bool[] _isDynamic = [];
    private int[] _bodyIndices = [];

    private int _count;
    private readonly List<int> _dynamicBodyIndices = new();

    #endregion

    #region 属性

    public int Count => _count;

    #endregion

    #region 公开方法

    public void Prepare(List<IRigidBody> bodies)
    {
        _count = bodies.Count;
        EnsureCapacity(_count);
        _dynamicBodyIndices.Clear();

        for (var i = 0; i < _count; i++)
        {
            var body = bodies[i];
            _positionsX[i] = body.Position.X;
            _positionsY[i] = body.Position.Y;
            _positionsZ[i] = body.Position.Z;
            _velocitiesX[i] = body.Velocity.X;
            _velocitiesY[i] = body.Velocity.Y;
            _velocitiesZ[i] = body.Velocity.Z;
            _forcesX[i] = 0f;
            _forcesY[i] = 0f;
            _forcesZ[i] = 0f;
            _masses[i] = body.Mass;
            _drags[i] = body.Drag;
            _useGravity[i] = body.UseGravity;
            _isDynamic[i] = body.BodyType == RigidBodyType.Dynamic && !body.IsKinematic;
            _bodyIndices[i] = i;

            if (_isDynamic[i])
            {
                _dynamicBodyIndices.Add(i);
            }
        }
    }

    public void Integrate(float delta, Vector3 gravity)
    {
        if (_count == 0)
        {
            return;
        }

        if (SimdBatch.IsSimdSupported && _dynamicBodyIndices.Count >= SimdBatch.VectorFloatCount)
        {
            IntegrateSimd(delta, gravity);
        }
        else
        {
            IntegrateScalar(delta, gravity);
        }
    }

    public void WriteBack(List<IRigidBody> bodies)
    {
        for (var i = 0; i < _count && i < bodies.Count; i++)
        {
            var body = bodies[i];
            body.Position = new Vector3(_positionsX[i], _positionsY[i], _positionsZ[i]);
            body.Velocity = new Vector3(_velocitiesX[i], _velocitiesY[i], _velocitiesZ[i]);
        }
    }

    #endregion

    #region 私有方法 - SIMD 积分

    private void IntegrateSimd(float delta, Vector3 gravity)
    {
        var dynamicCount = _dynamicBodyIndices.Count;
        var tempVelX = new float[dynamicCount];
        var tempVelY = new float[dynamicCount];
        var tempVelZ = new float[dynamicCount];
        var tempPosX = new float[dynamicCount];
        var tempPosY = new float[dynamicCount];
        var tempPosZ = new float[dynamicCount];

        for (var d = 0; d < dynamicCount; d++)
        {
            var i = _dynamicBodyIndices[d];
            tempVelX[d] = _velocitiesX[i];
            tempVelY[d] = _velocitiesY[i];
            tempVelZ[d] = _velocitiesZ[i];
            tempPosX[d] = _positionsX[i];
            tempPosY[d] = _positionsY[i];
            tempPosZ[d] = _positionsZ[i];
        }

        var gravityX = new float[dynamicCount];
        var gravityY = new float[dynamicCount];
        var gravityZ = new float[dynamicCount];

        for (var d = 0; d < dynamicCount; d++)
        {
            var i = _dynamicBodyIndices[d];
            gravityX[d] = _useGravity[i] && _masses[i] > 0f ? gravity.X * delta : 0f;
            gravityY[d] = _useGravity[i] && _masses[i] > 0f ? gravity.Y * delta : 0f;
            gravityZ[d] = _useGravity[i] && _masses[i] > 0f ? gravity.Z * delta : 0f;
        }

        var velSpanX = tempVelX.AsSpan();
        var velSpanY = tempVelY.AsSpan();
        var velSpanZ = tempVelZ.AsSpan();
        var gravSpanX = gravityX.AsSpan();
        var gravSpanY = gravityY.AsSpan();
        var gravSpanZ = gravityZ.AsSpan();

        SimdBatch.Add(velSpanX, gravSpanX, velSpanX);
        SimdBatch.Add(velSpanY, gravSpanY, velSpanY);
        SimdBatch.Add(velSpanZ, gravSpanZ, velSpanZ);

        var posSpanX = tempPosX.AsSpan();
        var posSpanY = tempPosY.AsSpan();
        var posSpanZ = tempPosZ.AsSpan();

        SimdBatch.MultiplyAdd(posSpanX, velSpanX, delta, posSpanX);
        SimdBatch.MultiplyAdd(posSpanY, velSpanY, delta, posSpanY);
        SimdBatch.MultiplyAdd(posSpanZ, velSpanZ, delta, posSpanZ);

        for (var d = 0; d < dynamicCount; d++)
        {
            var i = _dynamicBodyIndices[d];
            var dragFactor = MathF.Max(1f - _drags[i] * delta, 0f);
            _velocitiesX[i] = tempVelX[d] * dragFactor;
            _velocitiesY[i] = tempVelY[d] * dragFactor;
            _velocitiesZ[i] = tempVelZ[d] * dragFactor;
            _positionsX[i] = tempPosX[d];
            _positionsY[i] = tempPosY[d];
            _positionsZ[i] = tempPosZ[d];
        }
    }

    #endregion

    #region 私有方法 - 标量积分

    private void IntegrateScalar(float delta, Vector3 gravity)
    {
        foreach (var i in _dynamicBodyIndices)
        {
            if (_useGravity[i] && _masses[i] > 0f)
            {
                _velocitiesX[i] += gravity.X * delta;
                _velocitiesY[i] += gravity.Y * delta;
                _velocitiesZ[i] += gravity.Z * delta;
            }

            var dragFactor = MathF.Max(1f - _drags[i] * delta, 0f);
            _velocitiesX[i] *= dragFactor;
            _velocitiesY[i] *= dragFactor;
            _velocitiesZ[i] *= dragFactor;

            _positionsX[i] += _velocitiesX[i] * delta;
            _positionsY[i] += _velocitiesY[i] * delta;
            _positionsZ[i] += _velocitiesZ[i] * delta;
        }
    }

    #endregion

    #region 私有方法

    private void EnsureCapacity(int capacity)
    {
        if (_positionsX.Length >= capacity)
        {
            return;
        }

        var newCapacity = Math.Max(capacity, _positionsX.Length * 2);
        Array.Resize(ref _positionsX, newCapacity);
        Array.Resize(ref _positionsY, newCapacity);
        Array.Resize(ref _positionsZ, newCapacity);
        Array.Resize(ref _velocitiesX, newCapacity);
        Array.Resize(ref _velocitiesY, newCapacity);
        Array.Resize(ref _velocitiesZ, newCapacity);
        Array.Resize(ref _forcesX, newCapacity);
        Array.Resize(ref _forcesY, newCapacity);
        Array.Resize(ref _forcesZ, newCapacity);
        Array.Resize(ref _masses, newCapacity);
        Array.Resize(ref _drags, newCapacity);
        Array.Resize(ref _useGravity, newCapacity);
        Array.Resize(ref _isDynamic, newCapacity);
        Array.Resize(ref _bodyIndices, newCapacity);
    }

    #endregion
}
