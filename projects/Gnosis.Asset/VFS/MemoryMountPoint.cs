using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Gnosis.Asset.VFS;

/// <summary>
/// 内存文件系统挂载点，所有文件存储在内存中
/// </summary>
public class MemoryMountPoint : IMountPoint
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _directories = new(StringComparer.OrdinalIgnoreCase);

    public string MountPath { get; }
    public int Priority { get; }

    /// <summary>
    /// 初始化内存挂载点
    /// </summary>
    /// <param name="mountPath">虚拟挂载路径</param>
    /// <param name="priority">优先级</param>
    public MemoryMountPoint(string mountPath, int priority = 0)
    {
        MountPath = NormalizePath(mountPath);
        Priority = priority;

        EnsureDirectoryExists(MountPath);
    }

    /// <summary>
    /// 写入文件到内存
    /// </summary>
    /// <param name="virtualPath">虚拟路径</param>
    /// <param name="data">文件数据</param>
    public void WriteFile(string virtualPath, byte[] data)
    {
        var normalized = NormalizePath(virtualPath);
        EnsureParentDirectoryExists(normalized);
        _files[normalized] = data;
    }

    /// <summary>
    /// 从内存中删除文件
    /// </summary>
    /// <param name="virtualPath">虚拟路径</param>
    public void DeleteFile(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        _files.TryRemove(normalized, out _);
    }

    public bool FileExists(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        return _files.ContainsKey(normalized);
    }

    public Stream? OpenRead(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        if (_files.TryGetValue(normalized, out var data))
        {
            return new MemoryStream(data, writable: false);
        }

        return null;
    }

    public Stream? OpenWrite(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        EnsureParentDirectoryExists(normalized);
        return new MemoryWriteStream(this, normalized);
    }

    public IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption)
    {
        var normalizedDir = NormalizePath(virtualPath);
        var prefix = string.IsNullOrEmpty(normalizedDir) ? string.Empty : normalizedDir + "/";

        var regex = WildcardToRegex(searchPattern);

        foreach (var filePath in _files.Keys)
        {
            if (!filePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = filePath[prefix.Length..];
            if (string.IsNullOrEmpty(relativePath))
            {
                continue;
            }

            if (searchOption == SearchOption.TopDirectoryOnly && relativePath.Contains('/'))
            {
                continue;
            }

            var fileName = relativePath[(relativePath.LastIndexOf('/') + 1)..];
            if (regex.IsMatch(fileName))
            {
                yield return filePath;
            }
        }
    }

    public IEnumerable<string> EnumerateDirectories(string virtualPath)
    {
        var normalizedDir = NormalizePath(virtualPath);
        var prefix = string.IsNullOrEmpty(normalizedDir) ? string.Empty : normalizedDir + "/";

        foreach (var dirPath in _directories.Keys)
        {
            if (!dirPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = dirPath[prefix.Length..];
            if (string.IsNullOrEmpty(relativePath))
            {
                continue;
            }

            if (!relativePath.Contains('/'))
            {
                yield return dirPath;
            }
        }
    }

    public bool DirectoryExists(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        return _directories.ContainsKey(normalized);
    }

    public void CreateDirectory(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        EnsureDirectoryExists(normalized);
    }

    public void Dispose()
    {
        _files.Clear();
        _directories.Clear();
    }

    private void EnsureDirectoryExists(string normalizedPath)
    {
        _directories.TryAdd(normalizedPath, 0);

        var parent = GetParentPath(normalizedPath);
        while (!string.IsNullOrEmpty(parent) && !_directories.ContainsKey(parent))
        {
            _directories.TryAdd(parent, 0);
            parent = GetParentPath(parent);
        }
    }

    private void EnsureParentDirectoryExists(string normalizedPath)
    {
        var parent = GetParentPath(normalizedPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureDirectoryExists(parent);
        }
    }

    private static string? GetParentPath(string normalizedPath)
    {
        var lastSlash = normalizedPath.LastIndexOf('/');
        if (lastSlash < 0)
        {
            return null;
        }

        return normalizedPath[..lastSlash];
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private class MemoryWriteStream : MemoryStream
    {
        private readonly MemoryMountPoint _mountPoint;
        private readonly string _virtualPath;
        private bool _disposed;

        public MemoryWriteStream(MemoryMountPoint mountPoint, string virtualPath)
        {
            _mountPoint = mountPoint;
            _virtualPath = virtualPath;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _mountPoint._files[_virtualPath] = ToArray();
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
