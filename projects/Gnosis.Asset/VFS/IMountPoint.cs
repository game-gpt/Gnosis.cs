namespace Gnosis.Asset.VFS;

/// <summary>
/// 虚拟文件系统挂载点接口
/// </summary>
public interface IMountPoint : IDisposable
{
    /// <summary>
    /// 挂载路径（虚拟路径前缀）
    /// </summary>
    string MountPath { get; }

    /// <summary>
    /// 优先级（数值越高优先级越高）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    bool FileExists(string virtualPath);

    /// <summary>
    /// 打开文件读取流
    /// </summary>
    Stream? OpenRead(string virtualPath);

    /// <summary>
    /// 打开文件写入流
    /// </summary>
    Stream? OpenWrite(string virtualPath);

    /// <summary>
    /// 枚举文件
    /// </summary>
    IEnumerable<string> EnumerateFiles(string virtualPath, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// 枚举目录
    /// </summary>
    IEnumerable<string> EnumerateDirectories(string virtualPath);

    /// <summary>
    /// 判断目录是否存在
    /// </summary>
    bool DirectoryExists(string virtualPath);

    /// <summary>
    /// 创建目录
    /// </summary>
    void CreateDirectory(string virtualPath);

    /// <summary>
    /// 删除文件
    /// </summary>
    void DeleteFile(string virtualPath);
}
