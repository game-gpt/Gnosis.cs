using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

public sealed class Camera3D : ICamera
{
    #region 字段

    private Vector3 _position;
    private Vector3 _forward;
    private Vector3 _up;
    private float _fieldOfView;
    private float _nearPlane;
    private float _farPlane;
    private float _aspectRatio;
    private bool _viewMatrixDirty = true;
    private bool _projectionMatrixDirty = true;
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;

    #endregion

    #region 属性

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            _viewMatrixDirty = true;
        }
    }

    public Vector3 Forward
    {
        get => _forward;
        set
        {
            _forward = Vector3.Normalize(value);
            _viewMatrixDirty = true;
        }
    }

    public Vector3 Up
    {
        get => _up;
        set
        {
            _up = Vector3.Normalize(value);
            _viewMatrixDirty = true;
        }
    }

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(_forward, _up));

    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            _fieldOfView = Math.Clamp(value, 1.0f, 179.0f);
            _projectionMatrixDirty = true;
        }
    }

    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            _nearPlane = Math.Max(0.001f, value);
            _projectionMatrixDirty = true;
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            _farPlane = Math.Max(_nearPlane + 0.001f, value);
            _projectionMatrixDirty = true;
        }
    }

    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = Math.Max(0.001f, value);
            _projectionMatrixDirty = true;
        }
    }

    public Matrix4x4 ViewMatrix
    {
        get
        {
            if (_viewMatrixDirty)
            {
                _viewMatrix = Matrix4x4.CreateLookAt(_position, _position + _forward, _up);
                _viewMatrixDirty = false;
            }

            return _viewMatrix;
        }
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            if (_projectionMatrixDirty)
            {
                _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                    _fieldOfView * MathHelper.Deg2Rad,
                    _aspectRatio,
                    _nearPlane,
                    _farPlane);
                _projectionMatrixDirty = false;
            }

            return _projectionMatrix;
        }
    }

    public RenderPathFlag RenderPathFlags { get; set; } = RenderPathFlag.Raster;

    #endregion

    #region 构造函数

    public Camera3D()
    {
        _position = Vector3.Zero;
        _forward = Vector3Extensions.Forward;
        _up = Vector3Extensions.Up;
        _fieldOfView = 60.0f;
        _nearPlane = 0.1f;
        _farPlane = 1000.0f;
        _aspectRatio = 16.0f / 9.0f;
        _viewMatrix = Matrix4x4.Identity;
        _projectionMatrix = Matrix4x4.Identity;
    }

    public Camera3D(Vector3 position, Vector3 forward, Vector3 up, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        _position = position;
        _forward = Vector3.Normalize(forward);
        _up = Vector3.Normalize(up);
        _fieldOfView = Math.Clamp(fieldOfView, 1.0f, 179.0f);
        _aspectRatio = Math.Max(0.001f, aspectRatio);
        _nearPlane = Math.Max(0.001f, nearPlane);
        _farPlane = Math.Max(_nearPlane + 0.001f, farPlane);
        _viewMatrix = Matrix4x4.Identity;
        _projectionMatrix = Matrix4x4.Identity;
    }

    #endregion

    #region 公开方法

    public void LookAt(Vector3 target)
    {
        var direction = target - _position;
        if (direction.LengthSquared() > MathHelper.Epsilon)
        {
            _forward = Vector3.Normalize(direction);
            _viewMatrixDirty = true;
        }
    }

    public void LookAt(Vector3 eye, Vector3 target, Vector3 up)
    {
        _position = eye;
        _up = Vector3.Normalize(up);
        var direction = target - eye;
        if (direction.LengthSquared() > MathHelper.Epsilon)
        {
            _forward = Vector3.Normalize(direction);
        }

        _viewMatrixDirty = true;
    }

    public void Move(Vector3 offset)
    {
        _position += offset;
        _viewMatrixDirty = true;
    }

    public void MoveForward(float distance)
    {
        _position += _forward * distance;
        _viewMatrixDirty = true;
    }

    public void MoveRight(float distance)
    {
        _position += Right * distance;
        _viewMatrixDirty = true;
    }

    public void MoveUp(float distance)
    {
        _position += _up * distance;
        _viewMatrixDirty = true;
    }

    public void Rotate(float yaw, float pitch)
    {
        var yawRotation = Matrix4x4.CreateFromAxisAngle(_up, yaw * MathHelper.Deg2Rad);
        var right = Right;
        var pitchRotation = Matrix4x4.CreateFromAxisAngle(right, pitch * MathHelper.Deg2Rad);

        _forward = Vector3.Normalize(Vector3.Transform(_forward, yawRotation));
        _forward = Vector3.Normalize(Vector3.Transform(_forward, pitchRotation));
        _up = Vector3.Normalize(Vector3.Transform(_up, pitchRotation));
        _viewMatrixDirty = true;
    }

    public void UpdateAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public Frustum GetFrustum()
    {
        var frustum = new Frustum();
        frustum.Update(ViewMatrix * ProjectionMatrix);
        return frustum;
    }

    #endregion
}
