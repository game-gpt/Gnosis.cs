using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Gnosis.Asset.Format;

public class FormatRegistry : IFormatRegistry
{
    private readonly ConcurrentDictionary<FormatType, IFormatHandler> _handlers = new();
    private ImmutableArray<IFormatHandler> _handlerList = ImmutableArray<IFormatHandler>.Empty;
    private readonly object _listLock = new();

    public void RegisterHandler(IFormatHandler handler)
    {
        _handlers.AddOrUpdate(handler.SupportedFormat, handler, (_, _) => handler);

        lock (_listLock)
        {
            var builder = ImmutableArray.CreateBuilder<IFormatHandler>(_handlerList.Length + 1);
            var replaced = false;

            foreach (var existing in _handlerList)
            {
                if (existing.SupportedFormat == handler.SupportedFormat)
                {
                    builder.Add(handler);
                    replaced = true;
                }
                else
                {
                    builder.Add(existing);
                }
            }

            if (!replaced)
            {
                builder.Add(handler);
            }

            _handlerList = builder.ToImmutable();
        }
    }

    public void UnregisterHandler(FormatType formatType)
    {
        if (_handlers.TryRemove(formatType, out _))
        {
            lock (_listLock)
            {
                var builder = ImmutableArray.CreateBuilder<IFormatHandler>(_handlerList.Length - 1);

                foreach (var existing in _handlerList)
                {
                    if (existing.SupportedFormat != formatType)
                    {
                        builder.Add(existing);
                    }
                }

                _handlerList = builder.ToImmutable();
            }
        }
    }

    public IFormatHandler? GetHandler(FormatType formatType)
    {
        return _handlers.GetValueOrDefault(formatType);
    }

    public IFormatHandler? GetHandler(string path)
    {
        var snapshot = _handlerList;

        foreach (var handler in snapshot)
        {
            if (handler.CanHandle(path))
            {
                return handler;
            }
        }

        return null;
    }

    public IEnumerable<IFormatHandler> GetAllHandlers()
    {
        return _handlerList;
    }

    public bool HasHandler(FormatType formatType)
    {
        return _handlers.ContainsKey(formatType);
    }
}
