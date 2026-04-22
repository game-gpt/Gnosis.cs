namespace Gnosis.Animation.Event;

public struct AnimationEvent
{
    public string Name { get; init; }
    public float Time { get; init; }
    public string? StringParameter { get; init; }
    public float FloatParameter { get; init; }
    public int IntParameter { get; init; }
}

public sealed class AnimationEventReceiver : IAnimationEventReceiver
{
    #region 字段

    private readonly Dictionary<string, Action<AnimationEvent>> _handlers = new();

    #endregion

    #region IAnimationEventReceiver 实现

    public void OnAnimationEvent(AnimationEvent evt)
    {
        if (_handlers.TryGetValue(evt.Name, out var handler))
        {
            handler(evt);
        }
    }

    #endregion

    #region 公开方法

    public void RegisterHandler(string eventName, Action<AnimationEvent> handler)
    {
        _handlers[eventName] = handler;
    }

    public void UnregisterHandler(string eventName)
    {
        _handlers.Remove(eventName);
    }

    #endregion
}
