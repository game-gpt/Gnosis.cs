using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Gnosis.Core.IO;

/// <summary>
/// 内存映射文件视图，提供高效的内存读写操作
/// </summary>
public unsafe class GnosisMemoryMappedView : IDisposable
{
    private MemoryMappedViewAccessor _accessor;
    private byte* _pointer;
    private bool _disposed;

    /// <summary>
    /// 视图大小（字节）
    /// </summary>
    public long Size { get; }

    internal GnosisMemoryMappedView(MemoryMappedViewAccessor accessor, long size)
    {
        _accessor = accessor;
        Size = size;
        _pointer = null;

        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
    }

    /// <summary>
    /// 从指定偏移量读取字节跨度
    /// </summary>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">读取字节数</param>
    /// <returns>只读字节跨度</returns>
    public ReadOnlySpan<byte> ReadSpan(long offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, Size);

        return new ReadOnlySpan<byte>(_pointer + offset, count);
    }

    /// <summary>
    /// 将字节跨度写入指定偏移量
    /// </summary>
    /// <param name="offset">起始偏移量</param>
    /// <param name="data">要写入的字节数据</param>
    public void WriteSpan(long offset, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + data.Length, Size);

        var destination = new Span<byte>(_pointer + offset, data.Length);
        data.CopyTo(destination);
    }

    /// <summary>
    /// 从指定偏移量读取非托管类型值
    /// </summary>
    /// <typeparam name="T">非托管类型</typeparam>
    /// <param name="offset">起始偏移量</param>
    /// <returns>读取的值</returns>
    public T Read<T>(long offset) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + sizeof(T), Size);

        return MemoryMarshal.Read<T>(new ReadOnlySpan<byte>(_pointer + offset, sizeof(T)));
    }

    /// <summary>
    /// 将非托管类型值写入指定偏移量
    /// </summary>
    /// <typeparam name="T">非托管类型</typeparam>
    /// <param name="offset">起始偏移量</param>
    /// <param name="value">要写入的值</param>
    public void Write<T>(long offset, in T value) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + sizeof(T), Size);

        MemoryMarshal.Write(new Span<byte>(_pointer + offset, sizeof(T)), in value);
    }

    /// <summary>
    /// 释放视图资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放视图资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (_pointer != null)
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _pointer = null;
        }

        if (disposing)
        {
            _accessor.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~GnosisMemoryMappedView()
    {
        Dispose(false);
    }
}
