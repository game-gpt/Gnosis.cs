namespace Gnosis.Animation;

public class StubAnimationStateMachine : IAnimationStateMachine
{
    public string Name => throw new NotImplementedException("动画系统尚未实现");
    public IAnimationState CurrentState => throw new NotImplementedException("动画系统尚未实现");
    public IAnimationState? NextState => throw new NotImplementedException("动画系统尚未实现");
    public float TransitionProgress => throw new NotImplementedException("动画系统尚未实现");

    public void AddState(IAnimationState state)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void AddTransition(ITransition transition)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void RemoveState(string stateName)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetBool(string parameter, bool value)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetFloat(string parameter, float value)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetTrigger(string parameter)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void Update(float delta)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}
