using Gnosis.Asset.Bundle;

namespace Gnosis.Asset.VFS;

public class BundleMountPoint : IMountPoint
{
    private readonly string _bundlePath;
    private readonly byte[]? _encryptionKey;
    private readonly IEncryptionProvider? _encryptionProvider;
    private AssetBundleIndex? _index;
    private readonly Dictionary<string, AssetBundleIndexEntry> _entryMap = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public string MountPath { get; }
    public int Priority { get; }

    /// <summary>
    /// 初始化资产包挂载点
    /// </summary>
    public BundleMountPoint(string bundlePath, string mountPath, int priority = 0)
    {
        _bundlePath = bundlePath ?? throw new ArgumentNullException(nameof(bundlePath));
        MountPath = mountPath?.Replace('\\', '/').Trim('/') ?? throw new ArgumentNullException(nameof(mountPath));
        Priority = priority;

        LoadIndex();
    }

    /// <summary>
    /// 初始化加密资产包挂载点
    /// </summary>
    public BundleMountPoint(string bundlePath, string mountPath, byte[] encryptionKey, IEncryptionProvider? encryptionProvider = null, int priority = 0)
    {
        _bundlePath = bundlePath ?? throw new ArgumentNullException(nameof(bundlePath));
        MountPath = mountPath?.Replace('\\', '/').Trim('/') ?? throw new ArgumentNullException(nameof(mountPath));
        _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
        _encryptionProvider = encryptionProvider;
        Priority = priority;

        LoadIndex();
    }

    #region IMountPoint 实现

    /// <summary>
    /// 判断文件是否存在于资产包中
    /// </summary>
    public bool FileExists(string virtualPath)
    {
        var relativePath = GetRelativePath(virtualPath);
        return relativePath != null && _entryMap.ContainsKey(relativePath);
    }

    /// <summary>
    /// 从资产包中打开文件读取流
    /// </summary>
    public Stream? OpenRead(string virtualPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var relativePath = GetRelativePath(virtualPath);

        if (relativePath == null || !_entryMap.TryGetValue(relativePath, out var entry))
        {
            return null;
        }

        var data = AssetBundler.ReadAsset(_bundlePath, entry.VirtualPath, _encryptionKey, _encryptionProvider);
        return new MemoryStream(data);
    }

    /// <summary>
    /// 资产包不支持写入
    /// </summary>
    public Stream? OpenWrite(string virtualPath)
    {
        throw new NotSupportedException("资产包挂载点不支持写入操作");
    }

    /// <summary>
    /// 枚举资产包中的文件
    /// </summary>
    public IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var relativePath = GetRelativePath(virtualPath) ?? "";

        foreach (var entryPath in _entryMap.Keys)
        {
            if (!entryPath.StartsWith(relativePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (searchPattern != "*" && !MatchesPattern(entryPath, searchPattern))
            {
                continue;
            }

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                var afterPrefix = entryPath.Length > relativePath.Length
                    ? entryPath[relativePath.Length..].TrimStart('/')
                    : "";

                if (afterPrefix.Contains('/') && searchOption == SearchOption.TopDirectoryOnly)
                {
                    continue;
                }
            }

            var fullPath = string.IsNullOrEmpty(MountPath)
                ? entryPath
                : $"{MountPath}/{entryPath}";

            yield return fullPath;
        }
    }

    /// <summary>
    /// 枚举资产包中的目录
    /// </summary>
    public IEnumerable<string> EnumerateDirectories(string virtualPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var relativePath = GetRelativePath(virtualPath) ?? "";
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entryPath in _entryMap.Keys)
        {
            if (!entryPath.StartsWith(relativePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var afterPrefix = entryPath.Length > relativePath.Length
                ? entryPath[relativePath.Length..].TrimStart('/')
                : "";

            var slashIdx = afterPrefix.IndexOf('/');
            if (slashIdx >= 0)
            {
                var dir = afterPrefix[..slashIdx];
                var fullPath = string.IsNullOrEmpty(MountPath)
                    ? dir
                    : $"{MountPath}/{dir}";

                dirs.Add(fullPath);
            }
        }

        return dirs;
    }

    /// <summary>
    /// 判断目录是否存在于资产包中
    /// </summary>
    public bool DirectoryExists(string virtualPath)
    {
        var relativePath = GetRelativePath(virtualPath);
        if (relativePath == null) return false;

        foreach (var entryPath in _entryMap.Keys)
        {
            if (entryPath.StartsWith(relativePath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 资产包不支持创建目录
    /// </summary>
    public void CreateDirectory(string virtualPath)
    {
        throw new NotSupportedException("资产包挂载点不支持创建目录");
    }

    /// <summary>
    /// 资产包不支持删除文件
    /// </summary>
    public void DeleteFile(string virtualPath)
    {
        throw new NotSupportedException("资产包挂载点不支持删除文件");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _entryMap.Clear();
        _index = null;
        _disposed = true;
    }

    #endregion

    #region 私有方法

    private void LoadIndex()
    {
        if (!File.Exists(_bundlePath))
        {
            return;
        }

        try
        {
            _index = AssetBundler.LoadIndex(_bundlePath);

            if (_index?.Entries != null)
            {
                foreach (var entry in _index.Entries)
                {
                    _entryMap[entry.VirtualPath] = entry;
                }
            }
        }
        catch (InvalidDataException)
        {
        }
        catch (IOException)
        {
        }
    }

    private string? GetRelativePath(string virtualPath)
    {
        var normalized = virtualPath.Replace('\\', '/').Trim('/');

        if (string.IsNullOrEmpty(MountPath))
        {
            return normalized;
        }

        if (!normalized.StartsWith(MountPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(normalized, MountPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return "";
        }

        return normalized[(MountPath.Length + 1)..];
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        var fileName = Path.GetFileName(path);

        if (pattern == "*")
        {
            return true;
        }

        if (pattern.StartsWith("*"))
        {
            var suffix = pattern[1..];
            return fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
