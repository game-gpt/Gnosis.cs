using Gnosis.GameUI;

namespace Gnosis.GameUI;

public sealed class UIInputRouter : IUIInputRouter
{
    #region Fields

    private readonly Dictionary<UIInputLayer, List<Func<object, bool>>> _handlers = new();

    #endregion

    #region Public Methods

    public bool ProcessEvent(object inputEvent)
    {
        var layers = Enum.GetValues<UIInputLayer>()
            .OrderByDescending(l => (int)l);

        foreach (var layer in layers)
        {
            if (!_handlers.TryGetValue(layer, out var handlers))
            {
                continue;
            }

            foreach (var handler in handlers)
            {
                if (handler(inputEvent))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void RegisterHandler(UIInputLayer layer, Func<object, bool> handler)
    {
        if (!_handlers.TryGetValue(layer, out var handlers))
        {
            handlers = new List<Func<object, bool>>();
            _handlers[layer] = handlers;
        }

        handlers.Add(handler);
    }

    public void UnregisterHandler(UIInputLayer layer, Func<object, bool> handler)
    {
        if (_handlers.TryGetValue(layer, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    #endregion
}
