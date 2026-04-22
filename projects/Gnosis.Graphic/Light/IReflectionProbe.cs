namespace Gnosis.Graphic.Light;

public interface IReflectionProbe
{
    float[] Position { get; set; }
    float[] Size { get; set; }
    bool IsRealtime { get; set; }
    int Resolution { get; set; }
    float Importance { get; set; }
    float Intensity { get; set; }
    void Bake();
    void Update();
}
