using Gnosis.Core.ValueObjects;

namespace Gnosis.AntiCheat.Interfaces;

public interface IIntegrityChecker
{
    string Name { get; }
    bool Check();
    Timestamp LastCheckTime { get; }
}
