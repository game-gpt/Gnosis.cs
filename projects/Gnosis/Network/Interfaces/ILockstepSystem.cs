using Gnosis.Core.ValueObjects;

namespace Gnosis.Network.Interfaces;

public interface ILockstepSystem
{
    int TickRate { get; }
    int CurrentFrame { get; }
    
    void OnLockstepUpdate(int frame, IReadOnlyDictionary<int, byte[]> inputs);
    
    bool ReadyToAdvance { get; }
}
