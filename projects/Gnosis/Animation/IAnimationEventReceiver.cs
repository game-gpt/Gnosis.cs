using Gnosis.Assets.Formats;

namespace Gnosis.Animation;

public interface IAnimationEventReceiver
{
    void OnAnimationEvent(AnimationEvent animationEvent);
}
