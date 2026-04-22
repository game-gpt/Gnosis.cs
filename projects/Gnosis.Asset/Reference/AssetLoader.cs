using System.Collections.Concurrent;
using Gnosis.Asset.Bundle;
using Gnosis.Asset.VFS;

namespace Gnosis.Asset.Reference;

public sealed class AssetLoader : IAssetLoader, IDisposable
{
    private readonly IVirtualFileSystem _vfs;
    private readonly ConcurrentDictionary<string, AssetLoadState> _loadedAssets = new();
    private readonly ConcurrentDictionary<string, List<Action<IAssetHandle<object>>>> _pendingCallbacks = new();
    private readonly ConcurrentQueue<AssetLoadOperation> _loadQueue = new();
    private readonly object _lock = new();
    private bool _disposed;

    public int LoadingCount => _loadQueue.Count;

    public AssetLoader(IVirtualFileSystem vfs)
    {
        _vfs = vfs ?? throw new ArgumentNullException(nameof(vfs));
    }

    #region 加载资产

    /// <summary>
    /// 异步加载资产，返回资产句柄
    /// </summary>
    public IAssetHandle<T> LoadAsync<T>(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(path);

        if (_loadedAssets.TryGetValue(normalizedPath, out var existingState))
        {
            return CreateHandleFromState<T>(existingState);
        }

        var handle = new AssetHandle<T>(normalizedPath);

        var operation = new AssetLoadOperation
        {
            Path = normalizedPath,
            OnComplete = data =>
            {
                var state = new AssetLoadState
                {
                    Path = normalizedPath,
                    Data = data,
                    IsLoaded = true
                };

                _loadedAssets[normalizedPath] = state;

                handle.SetResult(data);

                if (_pendingCallbacks.TryRemove(normalizedPath, out var callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        var genericHandle = CreateHandleFromState<object>(state);
                        callback(genericHandle);
                    }
                }
            },
            OnError = error =>
            {
                handle.SetError(error);
            }
        };

        _loadQueue.Enqueue(operation);

        return handle;
    }

    /// <summary>
    /// 卸载指定路径的资产
    /// </summary>
    public void Unload(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(path);
        _loadedAssets.TryRemove(normalizedPath, out _);
    }

    /// <summary>
    /// 卸载所有已加载资产
    /// </summary>
    public void UnloadAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _loadedAssets.Clear();
    }

    /// <summary>
    /// 检查指定路径的资产是否已加载
    /// </summary>
    public bool IsLoaded(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(path);
        return _loadedAssets.TryGetValue(normalizedPath, out var state) && state.IsLoaded;
    }

    /// <summary>
    /// 获取已加载的资产
    /// </summary>
    public T? GetLoadedAsset<T>(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(path);

        if (!_loadedAssets.TryGetValue(normalizedPath, out var state) || !state.IsLoaded)
        {
            return default;
        }

        if (state.Data is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// 设置资产加载优先级
    /// </summary>
    public void SetPriority(string path, int priority)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// 更新加载队列，处理待加载资产
    /// </summary>
    public void Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (_loadQueue.TryDequeue(out var operation))
        {
            try
            {
                var data = LoadFromVfs(operation.Path);
                operation.OnComplete(data);
            }
            catch (FileNotFoundException ex)
            {
                operation.OnError(ex.Message);
            }
            catch (IOException ex)
            {
                operation.OnError(ex.Message);
            }
        }
    }

    #endregion

    #region 私有方法

    private byte[] LoadFromVfs(string path)
    {
        var stream = _vfs.OpenRead(path);

        if (stream == null)
        {
            throw new FileNotFoundException($"VFS 中未找到文件：{path}");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        stream.Dispose();
        return ms.ToArray();
    }

    private static IAssetHandle<T> CreateHandleFromState<T>(AssetLoadState state)
    {
        var handle = new AssetHandle<T>(state.Path);

        if (state.Data is T typed)
        {
            handle.SetResult(typed);
        }
        else
        {
            handle.SetResult(state.Data);
        }

        return handle;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _loadedAssets.Clear();
        _pendingCallbacks.Clear();

        while (_loadQueue.TryDequeue(out _))
        {
        }

        _disposed = true;
    }

    #endregion
}

internal sealed class AssetLoadState
{
    public string Path { get; init; } = string.Empty;
    public object? Data { get; init; }
    public bool IsLoaded { get; init; }
}

internal sealed class AssetLoadOperation
{
    public string Path { get; init; } = string.Empty;
    public Action<object?> OnComplete { get; init; } = _ => { };
    public Action<string> OnError { get; init; } = _ => { };
}

internal sealed class AssetHandle<T> : IAssetHandle<T>
{
    private readonly List<Action<IAssetHandle<T>>> _completeCallbacks = [];
    private readonly List<Action<float>> _progressCallbacks = [];
    private readonly object _lock = new();

    public string Path { get; }
    public bool IsDone { get; private set; }
    public bool IsFailed { get; private set; }
    public float Progress { get; private set; }
    public T? Asset { get; private set; }
    public string? Error { get; private set; }

    public AssetHandle(string path)
    {
        Path = path;
    }

    public void OnComplete(Action<IAssetHandle<T>> callback)
    {
        lock (_lock)
        {
            if (IsDone)
            {
                callback(this);
                return;
            }

            _completeCallbacks.Add(callback);
        }
    }

    public void OnProgress(Action<float> callback)
    {
        lock (_lock)
        {
            _progressCallbacks.Add(callback);
        }
    }

    internal void SetResult(object? data)
    {
        lock (_lock)
        {
            if (data is T typed)
            {
                Asset = typed;
            }

            IsDone = true;
            Progress = 1.0f;

            foreach (var callback in _completeCallbacks)
            {
                callback(this);
            }

            _completeCallbacks.Clear();
        }
    }

    internal void SetError(string error)
    {
        lock (_lock)
        {
            Error = error;
            IsDone = true;
            IsFailed = true;
            Progress = 1.0f;

            foreach (var callback in _completeCallbacks)
            {
                callback(this);
            }

            _completeCallbacks.Clear();
        }
    }
}
