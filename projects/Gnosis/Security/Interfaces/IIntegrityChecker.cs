using Gnosis.Core.ValueObjects;

namespace Gnosis.Security.Interfaces;

public interface IIntegrityChecker
{
    string Name { get; }
    bool Check();
    Timestamp LastCheckTime { get; }
}
