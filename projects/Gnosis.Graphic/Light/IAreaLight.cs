namespace Gnosis.Graphic.Light;

public interface IAreaLight : ILight
{
    float[] Position { get; set; }
    float[] Direction { get; set; }
    float Width { get; set; }
    float Height { get; set; }
    float Range { get; set; }
}
