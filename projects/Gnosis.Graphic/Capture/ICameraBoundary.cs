namespace Gnosis.Graphic.Capture;

public interface ICameraBoundary
{
    float[] MinBounds { get; set; }
    float[] MaxBounds { get; set; }
    bool UseSoftBounds { get; set; }
    float SoftBoundElasticity { get; set; }
    float[] ClampPosition(float[] position);
    bool IsWithinBounds(float[] position);
}
