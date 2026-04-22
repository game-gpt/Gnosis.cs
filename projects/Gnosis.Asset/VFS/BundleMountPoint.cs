namespace Gnosis.Asset.VFS;

/// <summary>
/// 资产包挂载点（存根实现，将在 M4 完整实现）
/// </summary>
public class BundleMountPoint : IMountPoint
{
    public string MountPath { get; }
    public int Priority { get; }

    /// <summary>
    /// 初始化资产包挂载点
    /// </summary>
    /// <param name="bundlePath">资产包路径</param>
    /// <param name="mountPath">虚拟挂载路径</param>
    /// <param name="priority">优先级</param>
    public BundleMountPoint(string bundlePath, string mountPath, int priority = 0)
    {
        MountPath = mountPath.Replace('\\', '/').Trim('/');
        Priority = priority;
    }

    public bool FileExists(string virtualPath)
    {
        return false;
    }

    public Stream? OpenRead(string virtualPath)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public Stream? OpenWrite(string virtualPath)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public IEnumerable<string> EnumerateDirectories(string virtualPath)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public bool DirectoryExists(string virtualPath)
    {
        return false;
    }

    public void CreateDirectory(string virtualPath)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public void DeleteFile(string virtualPath)
    {
        throw new NotImplementedException("资产包挂载点将在 M4 实现");
    }

    public void Dispose()
    {
    }
}
