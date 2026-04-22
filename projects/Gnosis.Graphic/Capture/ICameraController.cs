namespace Gnosis.Graphic.Capture;

public interface ICameraController
{
    float[] Position { get; set; }
    float[] Rotation { get; set; }
    float MoveSpeed { get; set; }
    float RotateSpeed { get; set; }
    bool IsEnabled { get; set; }
    void Update(float delta);
}
