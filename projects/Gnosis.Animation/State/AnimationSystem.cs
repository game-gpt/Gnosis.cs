using Gnosis.Animation.Blend;
using Gnosis.Animation.Clip;
using Gnosis.Animation.Event;
using Gnosis.Animation.IK;

namespace Gnosis.Animation.State;

public sealed class AnimationSystem : IAnimationSystem
{
    #region 字段

    private readonly List<IAnimator> _animators = new();

    #endregion

    #region IAnimationSystem 实现

    public IAnimator CreateAnimator(ISkeleton skeleton)
    {
        var animator = new Animator(skeleton);
        _animators.Add(animator);
        return animator;
    }

    public ISkeleton CreateSkeleton(string name, int boneCount)
    {
        return new Skeleton(name, boneCount);
    }

    public IAnimationStateMachine CreateStateMachine(string name)
    {
        return new AnimationStateMachine(name);
    }

    public IAnimationBlendTree CreateBlendTree(string name, BlendTreeType type)
    {
        return new AnimationBlendTree(name, type);
    }

    public IAnimationLayer CreateLayer(string name, float weight)
    {
        return new AnimationLayer(name, weight);
    }

    public IIKSolver CreateIKSolver(string name, IKConstraintType type)
    {
        return new IKSolver(name, type);
    }

    public void RegisterEventReceiver(IAnimator animator, IAnimationEventReceiver receiver)
    {
    }

    public void Update(float delta)
    {
        foreach (var animator in _animators)
        {
            animator.Update(delta);
        }
    }

    #endregion
}
