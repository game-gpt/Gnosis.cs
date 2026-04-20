using Gnosis.Renderer.Enums;

namespace Gnosis.Renderer.Interfaces;

public interface IResource : IDisposable
{
    ulong Id { get; }
    ResourceType ResourceType { get; }
    bool IsDisposed { get; }
}
