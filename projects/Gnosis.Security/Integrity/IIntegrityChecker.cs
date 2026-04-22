using Gnosis.Core.Time;

namespace Gnosis.Security.Integrity;

public interface IIntegrityChecker
{
    string Name { get; }
    bool Check();
    Timestamp LastCheckTime { get; }
}
