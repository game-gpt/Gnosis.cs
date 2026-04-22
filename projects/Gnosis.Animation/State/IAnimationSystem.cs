using Gnosis.Animation.Blend;
using Gnosis.Animation.Clip;
using Gnosis.Animation.Event;
using Gnosis.Animation.IK;

namespace Gnosis.Animation.State;

public interface IAnimationSystem
{
    IAnimator CreateAnimator(ISkeleton skeleton);
    ISkeleton CreateSkeleton(string name, int boneCount);
    IAnimationStateMachine CreateStateMachine(string name);
    IAnimationBlendTree CreateBlendTree(string name, BlendTreeType type);
    IAnimationLayer CreateLayer(string name, float weight);
    IIKSolver CreateIKSolver(string name, IKConstraintType type);
    void RegisterEventReceiver(IAnimator animator, IAnimationEventReceiver receiver);
    void Update(float delta);
}
