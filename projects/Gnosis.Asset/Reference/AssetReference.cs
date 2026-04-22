using System.Collections.Concurrent;

namespace Gnosis.Asset.Reference;

public sealed class AssetReference<T> : IAssetReference<T>, IDisposable
{
    private readonly IAssetLoader _loader;
    private readonly ConcurrentDictionary<string, AssetReference<T>> _references;
    private int _refCount;
    private bool _disposed;
    private readonly object _lock = new();

    public string Path { get; }
    public bool IsLoaded { get; private set; }
    public T? Asset { get; private set; }

    public AssetReference(string path, IAssetLoader loader)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _references = new ConcurrentDictionary<string, AssetReference<T>>();
        _refCount = 1;
    }

    /// <summary>
    /// 异步加载资产，返回资产句柄
    /// </summary>
    public IAssetHandle<T> LoadAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _refCount++;
        }

        var handle = _loader.LoadAsync<T>(Path);

        handle.OnComplete(h =>
        {
            lock (_lock)
            {
                if (h.IsDone && !h.IsFailed)
                {
                    Asset = h.Asset;
                    IsLoaded = true;
                }
            }
        });

        return handle;
    }

    /// <summary>
    /// 释放引用，减少引用计数
    /// </summary>
    public void Release()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_refCount > 0)
            {
                _refCount--;
            }

            if (_refCount <= 0 && IsLoaded)
            {
                _loader.Unload(Path);
                Asset = default;
                IsLoaded = false;
            }
        }
    }

    /// <summary>
    /// 获取当前引用计数
    /// </summary>
    public int RefCount
    {
        get
        {
            lock (_lock)
            {
                return _refCount;
            }
        }
    }

    /// <summary>
    /// 增加引用计数
    /// </summary>
    public void AddRef()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _refCount++;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (IsLoaded && _refCount > 0)
            {
                _loader.Unload(Path);
            }

            Asset = default;
            IsLoaded = false;
            _refCount = 0;
        }

        _disposed = true;
    }
}

public sealed class WeakAssetReference<T> where T : class
{
    private readonly string _path;
    private readonly IAssetLoader _loader;
    private WeakReference<T>? _weakRef;

    public string Path => _path;
    public bool IsLoaded
    {
        get
        {
            if (_weakRef == null)
            {
                return false;
            }

            return _weakRef.TryGetTarget(out _);
        }
    }

    public T? Asset
    {
        get
        {
            if (_weakRef != null && _weakRef.TryGetTarget(out var target))
            {
                return target;
            }

            return default;
        }
    }

    public WeakAssetReference(string path, IAssetLoader loader)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    /// <summary>
    /// 异步加载资产（弱引用），如果已被 GC 回收则重新加载
    /// </summary>
    public IAssetHandle<T> LoadAsync()
    {
        if (_weakRef != null && _weakRef.TryGetTarget(out var existing))
        {
            var cachedHandle = new CompletedAssetHandle<T>(_path, existing);
            return cachedHandle;
        }

        var handle = _loader.LoadAsync<T>(_path);

        handle.OnComplete(h =>
        {
            if (h.IsDone && !h.IsFailed && h.Asset != null)
            {
                _weakRef = new WeakReference<T>(h.Asset);
            }
        });

        return handle;
    }
}

internal sealed class CompletedAssetHandle<T> : IAssetHandle<T>
{
    public string Path { get; }
    public bool IsDone => true;
    public bool IsFailed => false;
    public float Progress => 1.0f;
    public T? Asset { get; }
    public string? Error => null;

    public CompletedAssetHandle(string path, T? asset)
    {
        Path = path;
        Asset = asset;
    }

    public void OnComplete(Action<IAssetHandle<T>> callback)
    {
        callback(this);
    }

    public void OnProgress(Action<float> callback)
    {
    }
}
