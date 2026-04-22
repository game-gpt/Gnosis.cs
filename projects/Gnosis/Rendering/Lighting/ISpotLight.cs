namespace Gnosis.Rendering.Lighting;

public interface ISpotLight : ILight
{
    float[] Position { get; set; }
    float[] Direction { get; set; }
    float Range { get; set; }
    float InnerConeAngle { get; set; }
    float OuterConeAngle { get; set; }
}
