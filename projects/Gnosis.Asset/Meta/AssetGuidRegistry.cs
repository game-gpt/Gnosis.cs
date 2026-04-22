using System.Collections.Concurrent;

namespace Gnosis.Asset.Meta;

public sealed class AssetGuidRegistry
{
    private readonly ConcurrentDictionary<AssetGuid, string> _guidToPath = new();
    private readonly ConcurrentDictionary<string, AssetGuid> _pathToGuid = new();

    public int Count => _guidToPath.Count;

    public AssetGuid Register(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("资产路径不能为空", nameof(path));
        }

        var normalizedPath = path.Replace('\\', '/');

        if (_pathToGuid.TryGetValue(normalizedPath, out var existingGuid))
        {
            return existingGuid;
        }

        var guid = AssetGuid.NewGuid();
        _guidToPath[guid] = normalizedPath;
        _pathToGuid[normalizedPath] = guid;
        return guid;
    }

    public AssetGuid Register(string path, AssetGuid guid)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("资产路径不能为空", nameof(path));
        }

        if (guid.IsEmpty)
        {
            throw new ArgumentException("不能使用空 GUID 注册资产", nameof(guid));
        }

        var normalizedPath = path.Replace('\\', '/');

        if (_pathToGuid.TryGetValue(normalizedPath, out var existingGuid) && existingGuid == guid)
        {
            return guid;
        }

        if (_guidToPath.TryGetValue(guid, out var existingPath) && existingPath != normalizedPath)
        {
            _pathToGuid.TryRemove(existingPath, out _);
        }

        if (_pathToGuid.TryGetValue(normalizedPath, out var oldGuid))
        {
            _guidToPath.TryRemove(oldGuid, out _);
        }

        _guidToPath[guid] = normalizedPath;
        _pathToGuid[normalizedPath] = guid;
        return guid;
    }

    public bool Unregister(AssetGuid guid)
    {
        if (_guidToPath.TryRemove(guid, out var path))
        {
            _pathToGuid.TryRemove(path, out _);
            return true;
        }

        return false;
    }

    public string GetPath(AssetGuid guid)
    {
        if (TryGetPath(guid, out var path))
        {
            return path;
        }

        throw new KeyNotFoundException($"未找到 GUID {guid} 对应的资产路径");
    }

    public AssetGuid GetGuid(string path)
    {
        var normalizedPath = path.Replace('\\', '/');

        if (TryGetGuid(normalizedPath, out var guid))
        {
            return guid;
        }

        throw new KeyNotFoundException($"未找到路径 {normalizedPath} 对应的 GUID");
    }

    public bool TryGetPath(AssetGuid guid, out string path)
    {
        return _guidToPath.TryGetValue(guid, out path!);
    }

    public bool TryGetGuid(string path, out AssetGuid guid)
    {
        var normalizedPath = path.Replace('\\', '/');
        return _pathToGuid.TryGetValue(normalizedPath, out guid);
    }

    public void Clear()
    {
        _guidToPath.Clear();
        _pathToGuid.Clear();
    }

    public IEnumerable<(AssetGuid Guid, string Path)> GetAllMappings()
    {
        foreach (var kvp in _guidToPath)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }
}
