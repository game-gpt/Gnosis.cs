namespace Gnosis.Animation;

public interface IBlendNode
{
    string Name { get; }
    string ClipName { get; }
    float[] Position { get; }
    float Speed { get; set; }
}

public enum BlendTreeType
{
    OneDimensional = 0,
    TwoDimensional = 1
}
