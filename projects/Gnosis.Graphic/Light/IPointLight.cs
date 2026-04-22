namespace Gnosis.Graphic.Light;

public interface IPointLight : ILight
{
    float[] Position { get; set; }
    float Range { get; set; }
    float Attenuation { get; set; }
}
