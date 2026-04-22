namespace Gnosis.Rendering.TwoD;

public interface ICamera2D
{
    float[] Position { get; set; }
    float Zoom { get; set; }
    float Rotation { get; set; }
    float[] ViewportSize { get; set; }
    float[] ScreenToWorld(float[] screenPoint);
    float[] WorldToScreen(float[] worldPoint);
}
