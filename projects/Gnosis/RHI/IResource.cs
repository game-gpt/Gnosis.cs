using GnosisEngine.RHI.Enums;

namespace GnosisEngine.RHI;

public interface IResource : IDisposable
{
    ulong Id { get; }
    ResourceType ResourceType { get; }
    bool IsDisposed { get; }
}
