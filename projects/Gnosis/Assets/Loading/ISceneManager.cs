namespace Gnosis.Assets.Loading;

public interface ISceneManager
{
    string? CurrentScene { get; }
    IReadOnlyList<string> LoadedScenes { get; }
    bool IsLoading { get; }
    float LoadingProgress { get; }
    void LoadScene(string sceneName);
    void LoadSceneAsync(string sceneName);
    void UnloadScene(string sceneName);
    void SwitchScene(string sceneName, ISceneTransition? transition = null);
    bool IsSceneLoaded(string sceneName);
}
