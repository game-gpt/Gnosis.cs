namespace Gnosis.Asset.Reference;

public sealed class SceneManager : ISceneManager, IDisposable
{
    private readonly IAssetLoader _assetLoader;
    private readonly Dictionary<string, SceneState> _scenes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _loadedSceneList = [];
    private readonly object _lock = new();
    private bool _disposed;

    public string? CurrentScene
    {
        get
        {
            lock (_lock)
            {
                foreach (var (name, state) in _scenes)
                {
                    if (state.IsActive)
                    {
                        return name;
                    }
                }

                return null;
            }
        }
    }

    public IReadOnlyList<string> LoadedScenes
    {
        get
        {
            lock (_lock)
            {
                return _loadedSceneList.ToList();
            }
        }
    }

    public bool IsLoading { get; private set; }
    public float LoadingProgress { get; private set; }

    public SceneManager(IAssetLoader assetLoader)
    {
        _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
    }

    #region 场景加载

    /// <summary>
    /// 同步加载场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_scenes.ContainsKey(sceneName))
            {
                return;
            }

            var state = new SceneState
            {
                Name = sceneName,
                IsLoaded = true,
                IsActive = _scenes.Count == 0
            };

            _scenes[sceneName] = state;
            _loadedSceneList.Add(sceneName);
        }
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IsLoading = true;
        LoadingProgress = 0f;

        Task.Run(() =>
        {
            try
            {
                LoadScene(sceneName);
                LoadingProgress = 1f;
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    /// <summary>
    /// 卸载场景
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (!_scenes.TryGetValue(sceneName, out var state))
            {
                return;
            }

            _scenes.Remove(sceneName);
            _loadedSceneList.Remove(sceneName);

            if (state.IsActive && _scenes.Count > 0)
            {
                var first = _scenes.First().Value;
                first.IsActive = true;
            }
        }
    }

    /// <summary>
    /// 切换场景（带可选过渡效果）
    /// </summary>
    public void SwitchScene(string sceneName, ISceneTransition? transition = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            foreach (var (name, state) in _scenes)
            {
                state.IsActive = false;
            }

            if (!_scenes.TryGetValue(sceneName, out var sceneState))
            {
                LoadScene(sceneName);

                if (_scenes.TryGetValue(sceneName, out sceneState))
                {
                    sceneState.IsActive = true;
                }
            }
            else
            {
                sceneState.IsActive = true;
            }
        }
    }

    /// <summary>
    /// 检查场景是否已加载
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            return _scenes.ContainsKey(sceneName);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _scenes.Clear();
            _loadedSceneList.Clear();
        }

        _disposed = true;
    }

    #endregion

    private sealed class SceneState
    {
        public string Name { get; init; } = string.Empty;
        public bool IsLoaded { get; set; }
        public bool IsActive { get; set; }
    }
}
