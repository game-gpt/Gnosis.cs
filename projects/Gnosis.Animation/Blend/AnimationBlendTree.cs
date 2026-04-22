using Gnosis.Animation.IK;

namespace Gnosis.Animation.Blend;

public sealed class AnimationBlendTree : IAnimationBlendTree
{
    #region 字段

    private readonly List<IBlendNode> _nodes = new();

    #endregion

    #region 属性

    public string Name { get; }

    public BlendTreeType Type { get; }

    public string ParameterName { get; set; } = "";

    public string ParameterNameY { get; set; } = "";

    public IReadOnlyList<IBlendNode> Nodes => _nodes;

    #endregion

    #region 构造函数

    public AnimationBlendTree(string name, BlendTreeType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
    }

    #endregion

    #region IAnimationBlendTree 实现

    public void AddNode(IBlendNode node)
    {
        _nodes.Add(node ?? throw new ArgumentNullException(nameof(node)));
    }

    public void RemoveNode(string nodeName)
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].Name == nodeName)
            {
                _nodes.RemoveAt(i);
                return;
            }
        }
    }

    public IBonePose[] Evaluate(float parameterX, float parameterY = 0f)
    {
        return [];
    }

    #endregion
}
