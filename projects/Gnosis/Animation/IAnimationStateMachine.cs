namespace Gnosis.Animation;

public interface IAnimationStateMachine
{
    string Name { get; }
    IAnimationState CurrentState { get; }
    IAnimationState? NextState { get; }
    float TransitionProgress { get; }
    void AddState(IAnimationState state);
    void RemoveState(string stateName);
    void AddTransition(ITransition transition);
    void SetFloat(string parameter, float value);
    void SetBool(string parameter, bool value);
    void SetTrigger(string parameter);
    void Update(float delta);
}
