namespace Gnosis.Network.Prediction;

/// <summary>
/// 状态同步系统接口
/// </summary>
public interface IStateSyncSystem
{
    void OnServerUpdate(float delta);
    void OnClientUpdate(float delta);
    void OnReceiveServerState(byte[] stateData);
    
    bool PredictionEnabled { get; set; }
}
