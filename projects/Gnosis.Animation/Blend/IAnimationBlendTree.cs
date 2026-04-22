using Gnosis.Animation.IK;

namespace Gnosis.Animation.Blend;

public interface IAnimationBlendTree
{
    string Name { get; }
    BlendTreeType Type { get; }
    string ParameterName { get; set; }
    string ParameterNameY { get; set; }
    IReadOnlyList<IBlendNode> Nodes { get; }
    void AddNode(IBlendNode node);
    void RemoveNode(string nodeName);
    IBonePose[] Evaluate(float parameterX, float parameterY = 0f);
}
