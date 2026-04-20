namespace Gnosis.Network;

public interface ILockstepSystem
{
    int TickRate { get; }
    int CurrentFrame { get; }
    
    void OnLockstepUpdate(int frame, IReadOnlyDictionary<int, byte[]> inputs);
    
    bool ReadyToAdvance { get; }
}
