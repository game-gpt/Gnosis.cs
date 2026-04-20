namespace Gnosis.Network;

public interface IStateSyncSystem
{
    void OnServerUpdate(float delta);
    void OnClientUpdate(float delta);
    void OnReceiveServerState(byte[] stateData);
    
    bool PredictionEnabled { get; set; }
}
