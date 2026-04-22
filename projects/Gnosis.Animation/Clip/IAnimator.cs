using Gnosis.Animation.IK;
using Gnosis.Animation.State;

namespace Gnosis.Animation.Clip;

public interface IAnimator
{
    ISkeleton? Skeleton { get; }
    IReadOnlyList<IAnimationLayer> Layers { get; }
    IAnimationStateMachine StateMachine { get; }
    void Play(string stateName);
    void PlayInFixedTime(string stateName, float fixedTime);
    void CrossFade(string stateName, float transitionDuration);
    void SetFloat(string name, float value);
    void SetBool(string name, bool value);
    void SetTrigger(string name);
    float GetFloat(string name);
    bool GetBool(string name);
    void AddLayer(IAnimationLayer layer);
    void AddIKSolver(IIKSolver solver);
    void Update(float delta);
}
