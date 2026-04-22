namespace Gnosis.Asset.Reference;

public interface IAssetLoader
{
    int LoadingCount { get; }
    IAssetHandle<T> LoadAsync<T>(string path);
    void Unload(string path);
    void UnloadAll();
    bool IsLoaded(string path);
    T? GetLoadedAsset<T>(string path);
    void SetPriority(string path, int priority);
    void Update();
}
