using Gnosis.Core.Math;

namespace Gnosis.Animation.Blend;

public interface IBlendNode
{
    string Name { get; }
    string ClipName { get; }
    Vector3 Position { get; }
    float Speed { get; set; }
}

public enum BlendTreeType
{
    OneDimensional = 0,
    TwoDimensional = 1
}
