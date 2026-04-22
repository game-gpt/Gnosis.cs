namespace Gnosis.Asset.VFS;

/// <summary>
/// 物理文件系统挂载点，将虚拟路径映射到磁盘路径
/// </summary>
public class PhysicalMountPoint : IMountPoint
{
    private readonly string _rootPath;

    public string MountPath { get; }
    public int Priority { get; }

    /// <summary>
    /// 初始化物理挂载点
    /// </summary>
    /// <param name="rootPath">物理根目录路径</param>
    /// <param name="mountPath">虚拟挂载路径</param>
    /// <param name="priority">优先级</param>
    public PhysicalMountPoint(string rootPath, string mountPath, int priority = 0)
    {
        _rootPath = Path.GetFullPath(rootPath);
        MountPath = NormalizePath(mountPath);
        Priority = priority;
    }

    /// <summary>
    /// 将虚拟路径转换为物理路径
    /// </summary>
    public string MapVirtualToPhysical(string virtualPath)
    {
        var normalized = NormalizePath(virtualPath);
        var relativePath = StripMountPrefix(normalized);
        return Path.GetFullPath(Path.Combine(_rootPath, relativePath));
    }

    /// <summary>
    /// 将物理路径转换为虚拟路径
    /// </summary>
    public string MapPhysicalToVirtual(string physicalPath)
    {
        var fullPath = Path.GetFullPath(physicalPath);
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"物理路径 '{physicalPath}' 不在根目录 '{_rootPath}' 内");
        }

        var relativePath = fullPath[_rootPath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(relativePath) ? MountPath : $"{MountPath}/{relativePath.Replace('\\', '/')}";
    }

    public bool FileExists(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        return File.Exists(physicalPath);
    }

    public Stream? OpenRead(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        if (!File.Exists(physicalPath))
        {
            throw new FileNotFoundException($"未找到文件：{virtualPath}", physicalPath);
        }

        return File.OpenRead(physicalPath);
    }

    public Stream? OpenWrite(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        var directory = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return File.Create(physicalPath);
    }

    public IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        if (!Directory.Exists(physicalPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateFiles(physicalPath, searchPattern, searchOption)
            .Select(MapPhysicalToVirtual);
    }

    public IEnumerable<string> EnumerateDirectories(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        if (!Directory.Exists(physicalPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateDirectories(physicalPath)
            .Select(MapPhysicalToVirtual);
    }

    public bool DirectoryExists(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        return Directory.Exists(physicalPath);
    }

    public void CreateDirectory(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        Directory.CreateDirectory(physicalPath);
    }

    public void DeleteFile(string virtualPath)
    {
        var physicalPath = MapVirtualToPhysical(virtualPath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }

    public void Dispose()
    {
    }

    private string StripMountPrefix(string normalizedPath)
    {
        if (string.IsNullOrEmpty(MountPath))
        {
            return normalizedPath;
        }

        if (normalizedPath == MountPath)
        {
            return string.Empty;
        }

        var prefix = MountPath + "/";
        if (normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath[prefix.Length..];
        }

        return normalizedPath;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }
}
