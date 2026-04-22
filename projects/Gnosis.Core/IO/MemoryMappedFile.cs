using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using MMFA = System.IO.MemoryMappedFiles.MemoryMappedFileAccess;

namespace Gnosis.Core.IO;

/// <summary>
/// 内存映射文件抽象，封装 System.IO.MemoryMappedFiles 操作
/// </summary>
public class GnosisMemoryMappedFile : IDisposable
{
    private MemoryMappedFile _mappedFile;
    private bool _disposed;

    /// <summary>
    /// 映射容量（字节）
    /// </summary>
    public long Capacity { get; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string Path { get; }

    private GnosisMemoryMappedFile(MemoryMappedFile mappedFile, string path, long capacity)
    {
        _mappedFile = mappedFile;
        Path = path;
        Capacity = capacity;
    }

    /// <summary>
    /// 创建内存映射文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="capacity">映射容量（字节）</param>
    /// <param name="access">访问模式</param>
    /// <returns>内存映射文件实例</returns>
    public static GnosisMemoryMappedFile Create(string path, long capacity, MemoryMappedFileAccess access)
    {
        var systemAccess = ConvertAccess(access);

        var fileStream = new FileStream(
            path,
            FileMode.OpenOrCreate,
            access == MemoryMappedFileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite,
            FileShare.ReadWrite,
            4096,
            FileOptions.None);

        if (fileStream.Length < capacity)
        {
            fileStream.SetLength(capacity);
        }

        var mappedFile = MemoryMappedFile.CreateFromFile(
            fileStream,
            null,
            capacity,
            systemAccess,
            HandleInheritability.None,
            leaveOpen: false);

        return new GnosisMemoryMappedFile(mappedFile, path, capacity);
    }

    /// <summary>
    /// 创建内存映射文件视图
    /// </summary>
    /// <param name="offset">起始偏移量</param>
    /// <param name="size">视图大小（字节）</param>
    /// <returns>内存映射视图</returns>
    public GnosisMemoryMappedView CreateView(long offset, long size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var accessor = _mappedFile.CreateViewAccessor(offset, size);
        return new GnosisMemoryMappedView(accessor, size);
    }

    /// <summary>
    /// 释放内存映射文件资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放内存映射文件资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _mappedFile.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~GnosisMemoryMappedFile()
    {
        Dispose(false);
    }

    private static MMFA ConvertAccess(MemoryMappedFileAccess access)
    {
        return access switch
        {
            MemoryMappedFileAccess.Read => MMFA.Read,
            MemoryMappedFileAccess.Write => MMFA.Write,
            MemoryMappedFileAccess.ReadWrite => MMFA.ReadWrite,
            _ => MMFA.ReadWrite
        };
    }
}
