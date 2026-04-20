using Gnosis.Rendering.Enums;

namespace Gnosis.Rendering.Interfaces;

public interface IResource : IDisposable
{
    ulong Id { get; }
    ResourceType ResourceType { get; }
    bool IsDisposed { get; }
}
