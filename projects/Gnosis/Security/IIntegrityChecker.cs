using Gnosis.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Security;

public interface IIntegrityChecker
{
    string Name { get; }
    bool Check();
    Timestamp LastCheckTime { get; }
}
