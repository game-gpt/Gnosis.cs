using Gnosis.Formats.Enums;

namespace Gnosis.Formats;

public class FormatRegistry : IFormatRegistry
{
    private readonly Dictionary<FormatType, IFormatHandler> _handlers = new();
    private readonly List<IFormatHandler> _handlerList = new();
    
    public void RegisterHandler(IFormatHandler handler)
    {
        if (_handlers.ContainsKey(handler.SupportedFormat))
        {
            _handlers[handler.SupportedFormat] = handler;
        }
        else
        {
            _handlers.Add(handler.SupportedFormat, handler);
            _handlerList.Add(handler);
        }
    }
    
    public void UnregisterHandler(FormatType formatType)
    {
        if (_handlers.TryGetValue(formatType, out var handler))
        {
            _handlers.Remove(formatType);
            _handlerList.Remove(handler);
        }
    }
    
    public IFormatHandler? GetHandler(FormatType formatType)
    {
        return _handlers.TryGetValue(formatType, out var handler) ? handler : null;
    }
    
    public IFormatHandler? GetHandler(string path)
    {
        return _handlerList.FirstOrDefault(h => h.CanHandle(path));
    }
    
    public IEnumerable<IFormatHandler> GetAllHandlers()
    {
        return _handlerList.AsReadOnly();
    }
    
    public bool HasHandler(FormatType formatType)
    {
        return _handlers.ContainsKey(formatType);
    }
}
