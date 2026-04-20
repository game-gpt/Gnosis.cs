using Gnosis.Core.ValueObjects;

namespace Gnosis.Security;

public interface IIntegrityChecker
{
    string Name { get; }
    bool Check();
    Timestamp LastCheckTime { get; }
}
