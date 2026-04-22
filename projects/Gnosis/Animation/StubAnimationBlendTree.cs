namespace Gnosis.Animation;

public class StubAnimationBlendTree : IAnimationBlendTree
{
    public string Name => throw new NotImplementedException("动画系统尚未实现");
    public BlendTreeType Type => throw new NotImplementedException("动画系统尚未实现");
    public string ParameterName { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
    public string ParameterNameY { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
    public IReadOnlyList<IBlendNode> Nodes => throw new NotImplementedException("动画系统尚未实现");

    public void AddNode(IBlendNode node)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IBonePose[] Evaluate(float parameterX, float parameterY = 0f)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void RemoveNode(string nodeName)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}
