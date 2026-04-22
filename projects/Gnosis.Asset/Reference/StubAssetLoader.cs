namespace Gnosis.Asset.Reference;

public class StubAssetLoader : IAssetLoader
{
    public int LoadingCount => throw new NotImplementedException("资源加载系统尚未实现");
    public IAssetHandle<T> LoadAsync<T>(string path) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void Unload(string path) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void UnloadAll() { throw new NotImplementedException("资源加载系统尚未实现"); }
    public bool IsLoaded(string path) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public T? GetLoadedAsset<T>(string path) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void SetPriority(string path, int priority) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void Update() { throw new NotImplementedException("资源加载系统尚未实现"); }
}
