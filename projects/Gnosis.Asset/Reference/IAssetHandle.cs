namespace Gnosis.Asset.Reference;

public interface IAssetHandle<T>
{
    string Path { get; }
    bool IsDone { get; }
    bool IsFailed { get; }
    float Progress { get; }
    T? Asset { get; }
    string? Error { get; }
    void OnComplete(Action<IAssetHandle<T>> callback);
    void OnProgress(Action<float> callback);
}
