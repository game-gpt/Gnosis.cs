namespace Gnosis.Animation;

public class StubAnimator : IAnimator
{
    public ISkeleton? Skeleton => throw new NotImplementedException("动画系统尚未实现");
    public IReadOnlyList<IAnimationLayer> Layers => throw new NotImplementedException("动画系统尚未实现");
    public IAnimationStateMachine StateMachine => throw new NotImplementedException("动画系统尚未实现");

    public void AddIKSolver(IIKSolver solver)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void AddLayer(IAnimationLayer layer)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void CrossFade(string stateName, float transitionDuration)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public bool GetBool(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public float GetFloat(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void Play(string stateName)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void PlayInFixedTime(string stateName, float fixedTime)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetBool(string name, bool value)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetFloat(string name, float value)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetTrigger(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void Update(float delta)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}
