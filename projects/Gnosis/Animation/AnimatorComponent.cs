using Gnosis.Animation;
using Gnosis.ECS.Core;

namespace Gnosis.Animation;

public struct AnimatorComponent : IComponent
{
    public IAnimator? Animator { get; set; }
    public string DefaultState { get; set; }
    public float PlaybackSpeed { get; set; }
}
