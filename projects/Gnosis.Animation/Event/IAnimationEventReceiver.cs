namespace Gnosis.Animation.Event;

public interface IAnimationEventReceiver
{
    void OnAnimationEvent(AnimationEvent animationEvent);
}
