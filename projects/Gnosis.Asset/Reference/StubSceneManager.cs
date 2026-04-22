namespace Gnosis.Asset.Reference;

public class StubSceneManager : ISceneManager
{
    public string? CurrentScene => throw new NotImplementedException("资源加载系统尚未实现");
    public IReadOnlyList<string> LoadedScenes => throw new NotImplementedException("资源加载系统尚未实现");
    public bool IsLoading => throw new NotImplementedException("资源加载系统尚未实现");
    public float LoadingProgress => throw new NotImplementedException("资源加载系统尚未实现");
    public bool IsSceneLoaded(string sceneName) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void LoadScene(string sceneName) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void LoadSceneAsync(string sceneName) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void SwitchScene(string sceneName, ISceneTransition? transition = null) { throw new NotImplementedException("资源加载系统尚未实现"); }
    public void UnloadScene(string sceneName) { throw new NotImplementedException("资源加载系统尚未实现"); }
}
