namespace Gnosis.Animation;

public class StubAnimationLayer : IAnimationLayer
{
    public string Name => throw new NotImplementedException("动画系统尚未实现");
    public float Weight { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
    public IAnimationStateMachine StateMachine => throw new NotImplementedException("动画系统尚未实现");
    public string? MaskName { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
    public bool IsAdditive { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
    public bool IsEnabled { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }
}
