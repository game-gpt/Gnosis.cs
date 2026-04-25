using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sprite2D;

public sealed class Camera2D : ICamera2D, IView
{
    #region 字段

    private Vector3 _position = Vector3.Zero;
    private float _zoom = 1.0f;
    private float _rotation;
    private Vector2 _viewportSize = new(1280, 720);

    #endregion

    #region 属性

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Zoom
    {
        get => _zoom;
        set => _zoom = Math.Max(0.01f, value);
    }

    public float Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public Vector2 ViewportSize
    {
        get => _viewportSize;
        set => _viewportSize = value;
    }

    public Matrix4x4 ViewMatrix
    {
        get
        {
            var translation = Matrix4x4.CreateTranslation(-_position.X, -_position.Y, 0);
            var rotation = Matrix4x4.CreateRotationZ(-_rotation);
            var scale = Matrix4x4.CreateScale(_zoom, _zoom, 1.0f);
            var origin = Matrix4x4.CreateTranslation(_viewportSize.X / 2.0f, _viewportSize.Y / 2.0f, 0);
            return translation * rotation * scale * origin;
        }
    }

    public Matrix4x4 ProjectionMatrix => Matrix4x4.CreateOrthographicOffCenter(
        0, _viewportSize.X, _viewportSize.Y, 0, -1.0f, 1.0f);

    public RenderPathFlag RenderPathFlags => RenderPathFlag.Raster;

    #endregion

    #region 坐标变换

    public Vector3 ScreenToWorld(Vector3 screenPoint)
    {
        var viewProj = ViewMatrix * ProjectionMatrix;
        if (!Matrix4x4.Invert(viewProj, out var inverse))
        {
            return screenPoint;
        }

        var world = Vector4.Transform(new Vector4(screenPoint.X, screenPoint.Y, 0.0f, 1.0f), inverse);
        return new Vector3(world.X / world.W, world.Y / world.W, screenPoint.Z);
    }

    public Vector3 WorldToScreen(Vector3 worldPoint)
    {
        var viewProj = ViewMatrix * ProjectionMatrix;
        var screen = Vector4.Transform(new Vector4(worldPoint.X, worldPoint.Y, 0.0f, 1.0f), viewProj);
        return new Vector3(screen.X / screen.W, screen.Y / screen.W, worldPoint.Z);
    }

    #endregion

    #region 公开方法

    public void LookAt(Vector2 worldPosition)
    {
        _position = new Vector3(worldPosition, _position.Z);
    }

    public void LookAt(float x, float y)
    {
        _position = new Vector3(x, y, _position.Z);
    }

    #endregion
}
