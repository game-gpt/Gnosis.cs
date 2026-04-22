namespace Gnosis.Animation;

public interface IBonePose
{
    string BoneName { get; }
    float[] Position { get; }
    float[] Rotation { get; }
    float[] Scale { get; }
}
