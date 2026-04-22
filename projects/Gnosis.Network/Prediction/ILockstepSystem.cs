namespace Gnosis.Network.Prediction;

/// <summary>
/// 帧同步系统接口
/// </summary>
public interface ILockstepSystem
{
    int TickRate { get; }
    int CurrentFrame { get; }
    
    void OnLockstepUpdate(int frame, IReadOnlyDictionary<int, byte[]> inputs);
    
    bool ReadyToAdvance { get; }
}
