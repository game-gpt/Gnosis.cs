namespace Gnosis.Assets.Loading;

public interface IAssetReference<T>
{
    string Path { get; }
    bool IsLoaded { get; }
    T? Asset { get; }
    IAssetHandle<T> LoadAsync();
    void Release();
}
