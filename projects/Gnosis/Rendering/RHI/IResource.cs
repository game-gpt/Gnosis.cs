namespace Gnosis.Rendering.RHI;

public interface IResource : IDisposable
{
    ulong Id { get; }
    ResourceType ResourceType { get; }
    bool IsDisposed { get; }
}
