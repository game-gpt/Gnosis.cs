namespace Gnosis.Asset.VFS;

/// <summary>
/// 虚拟文件系统实现，支持多挂载点按优先级叠加
/// </summary>
public class VirtualFileSystem : IVirtualFileSystem, IDisposable
{
    private readonly List<IMountPoint> _mountPoints = new();
    private readonly object _lock = new();

    #region IVirtualFileSystem 实现

    /// <summary>
    /// 通过路径和文件系统实例挂载
    /// </summary>
    public void Mount(string path, IFileSystem fileSystem)
    {
        var mountPoint = new FileSystemMountPoint(path, fileSystem);
        Mount(mountPoint);
    }

    /// <summary>
    /// 卸载指定路径的挂载点
    /// </summary>
    public void Unmount(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            var index = _mountPoints.FindIndex(mp => mp.MountPath == normalizedPath);
            if (index >= 0)
            {
                var mountPoint = _mountPoints[index];
                _mountPoints.RemoveAt(index);
                mountPoint.Dispose();
            }
        }
    }

    /// <summary>
    /// 打开文件读取流，按优先级查找第一个包含该文件的挂载点
    /// </summary>
    public Stream? OpenRead(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (!IsPathUnderMount(normalizedPath, mountPoint.MountPath))
                {
                    continue;
                }

                if (mountPoint.FileExists(normalizedPath))
                {
                    return mountPoint.OpenRead(normalizedPath);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 打开文件写入流，按优先级查找第一个匹配的挂载点
    /// </summary>
    public Stream? OpenWrite(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (IsPathUnderMount(normalizedPath, mountPoint.MountPath))
                {
                    return mountPoint.OpenWrite(normalizedPath);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 判断文件是否存在于任意挂载点
    /// </summary>
    public bool FileExists(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (IsPathUnderMount(normalizedPath, mountPoint.MountPath) && mountPoint.FileExists(normalizedPath))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断目录是否存在于任意挂载点
    /// </summary>
    public bool DirectoryExists(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (IsPathUnderMount(normalizedPath, mountPoint.MountPath) && mountPoint.DirectoryExists(normalizedPath))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 在优先级最高的匹配挂载点中创建目录
    /// </summary>
    public void CreateDirectory(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (IsPathUnderMount(normalizedPath, mountPoint.MountPath))
                {
                    mountPoint.CreateDirectory(normalizedPath);
                    return;
                }
            }
        }

        throw new DirectoryNotFoundException($"未找到匹配的挂载点以创建目录：{path}");
    }

    /// <summary>
    /// 在包含该文件的挂载点中删除文件
    /// </summary>
    public void DeleteFile(string path)
    {
        var normalizedPath = NormalizePath(path);
        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (IsPathUnderMount(normalizedPath, mountPoint.MountPath) && mountPoint.FileExists(normalizedPath))
                {
                    mountPoint.DeleteFile(normalizedPath);
                    return;
                }
            }
        }

        throw new FileNotFoundException($"未找到文件以删除：{path}", path);
    }

    /// <summary>
    /// 从所有匹配挂载点聚合枚举文件
    /// </summary>
    public IEnumerable<string> GetFiles(string path, string searchPattern = "*")
    {
        var normalizedPath = NormalizePath(path);
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (!IsPathUnderMount(normalizedPath, mountPoint.MountPath))
                {
                    continue;
                }

                foreach (var file in mountPoint.EnumerateFiles(normalizedPath, searchPattern, SearchOption.TopDirectoryOnly))
                {
                    if (seen.Add(file))
                    {
                        results.Add(file);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 从所有匹配挂载点聚合枚举目录
    /// </summary>
    public IEnumerable<string> GetDirectories(string path)
    {
        var normalizedPath = NormalizePath(path);
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        lock (_lock)
        {
            foreach (var mountPoint in GetSortedMountPoints())
            {
                if (!IsPathUnderMount(normalizedPath, mountPoint.MountPath))
                {
                    continue;
                }

                foreach (var dir in mountPoint.EnumerateDirectories(normalizedPath))
                {
                    if (seen.Add(dir))
                    {
                        results.Add(dir);
                    }
                }
            }
        }

        return results;
    }

    #endregion

    #region 扩展方法

    /// <summary>
    /// 挂载一个挂载点实例
    /// </summary>
    public void Mount(IMountPoint mountPoint)
    {
        lock (_lock)
        {
            _mountPoints.Add(mountPoint);
        }
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var mountPoint in _mountPoints)
            {
                mountPoint.Dispose();
            }

            _mountPoints.Clear();
        }
    }

    #endregion

    #region 私有方法

    private List<IMountPoint> GetSortedMountPoints()
    {
        return _mountPoints.OrderByDescending(mp => mp.Priority).ToList();
    }

    private static bool IsPathUnderMount(string normalizedPath, string mountPath)
    {
        if (string.IsNullOrEmpty(mountPath))
        {
            return true;
        }

        if (normalizedPath == mountPath)
        {
            return true;
        }

        return normalizedPath.StartsWith(mountPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>();

        foreach (var segment in segments)
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                continue;
            }

            stack.Add(segment);
        }

        return string.Join("/", stack);
    }

    #endregion

    private class FileSystemMountPoint : IMountPoint
    {
        private readonly IFileSystem _fileSystem;

        public string MountPath { get; }
        public int Priority { get; }

        public FileSystemMountPoint(string mountPath, IFileSystem fileSystem, int priority = 0)
        {
            MountPath = mountPath.Replace('\\', '/').Trim('/');
            _fileSystem = fileSystem;
            Priority = priority;
        }

        public bool FileExists(string virtualPath)
        {
            return _fileSystem.FileExists(virtualPath);
        }

        public Stream? OpenRead(string virtualPath)
        {
            return _fileSystem.OpenRead(virtualPath);
        }

        public Stream? OpenWrite(string virtualPath)
        {
            return _fileSystem.OpenWrite(virtualPath);
        }

        public bool DirectoryExists(string virtualPath)
        {
            return _fileSystem.DirectoryExists(virtualPath);
        }

        public void CreateDirectory(string virtualPath)
        {
            _fileSystem.CreateDirectory(virtualPath);
        }

        public void DeleteFile(string virtualPath)
        {
            _fileSystem.DeleteFile(virtualPath);
        }

        public IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption)
        {
            return _fileSystem.GetFiles(virtualPath, searchPattern);
        }

        public IEnumerable<string> EnumerateDirectories(string virtualPath)
        {
            return _fileSystem.GetDirectories(virtualPath);
        }

        public void Dispose()
        {
        }
    }
}
