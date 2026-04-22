namespace Gnosis.Scene.Stream;

public sealed class SceneStreamer
{
    #region 字段

    private readonly HashSet<string> _loadedScenes = new();
    private readonly Dictionary<string, float> _scenePriorities = new();

    #endregion

    #region 属性

    public int LoadedSceneCount => _loadedScenes.Count;

    #endregion

    #region 公开方法

    public void LoadScene(string scenePath, float priority = 0f)
    {
        _loadedScenes.Add(scenePath);
        _scenePriorities[scenePath] = priority;
    }

    public void UnloadScene(string scenePath)
    {
        _loadedScenes.Remove(scenePath);
        _scenePriorities.Remove(scenePath);
    }

    public bool IsSceneLoaded(string scenePath)
    {
        return _loadedScenes.Contains(scenePath);
    }

    public void SetPriority(string scenePath, float priority)
    {
        if (_scenePriorities.ContainsKey(scenePath))
        {
            _scenePriorities[scenePath] = priority;
        }
    }

    #endregion
}
