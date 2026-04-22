using Gnosis.Animation.Blend;
using Gnosis.Animation.Clip;
using Gnosis.Animation.Event;
using Gnosis.Animation.IK;

namespace Gnosis.Animation.State;

public class StubAnimationSystem : IAnimationSystem
{
    public IAnimator CreateAnimator(ISkeleton skeleton)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public ISkeleton CreateSkeleton(string name, int boneCount)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IAnimationStateMachine CreateStateMachine(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IAnimationBlendTree CreateBlendTree(string name, BlendTreeType type)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IAnimationLayer CreateLayer(string name, float weight)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IIKSolver CreateIKSolver(string name, IKConstraintType type)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void RegisterEventReceiver(IAnimator animator, IAnimationEventReceiver receiver)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void Update(float delta)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}
