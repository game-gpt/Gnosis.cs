using System.IO;
using global::System.Threading.Tasks;

namespace Gnosis.Core.IO;

/// <summary>
/// 操作系统文件系统实现，基于 System.IO 提供物理文件操作
/// </summary>
public class PhysicalFileSystem : IFileSystem
{
    /// <summary>
    /// 以只读方式打开文件流
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件流</returns>
    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    /// <summary>
    /// 以写入方式打开文件流，若文件不存在则创建
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件流</returns>
    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件是否存在</returns>
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// 判断目录是否存在
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>目录是否存在</returns>
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <summary>
    /// 创建目录，若已存在则不执行操作
    /// </summary>
    /// <param name="path">目录路径</param>
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">文件路径</param>
    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    /// <summary>
    /// 删除目录
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="recursive">是否递归删除子目录和文件</param>
    public void DeleteDirectory(string path, bool recursive)
    {
        Directory.Delete(path, recursive);
    }

    /// <summary>
    /// 获取目录下的文件列表
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="searchPattern">搜索模式，默认为 "*"</param>
    /// <returns>文件路径数组</returns>
    public string[] GetFiles(string path, string searchPattern = "*")
    {
        return Directory.GetFiles(path, searchPattern);
    }

    /// <summary>
    /// 获取目录下的子目录列表
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>子目录路径数组</returns>
    public string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    /// <summary>
    /// 以异步方式打开文件只读流
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件流</returns>
    public Task<Stream> OpenReadAsync(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<Stream>(stream);
    }

    /// <summary>
    /// 以异步方式打开文件写入流，若文件不存在则创建
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件流</returns>
    public Task<Stream> OpenWriteAsync(string path)
    {
        var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        return Task.FromResult<Stream>(stream);
    }
}
