namespace Gnosis.Graphic.Light;

public interface ILightProbe
{
    float[] Position { get; set; }
    float[] ShCoefficients { get; }
    void Bake();
    void Update();
}
