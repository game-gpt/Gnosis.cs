using System;
using System.IO;

namespace Gnosis.Core.IO;

/// <summary>
/// 基于字节数组的可寻址流，提供内存中的流读写操作
/// </summary>
public class VirtualFileStream : Stream
{
    private byte[] _buffer;
    private int _length;
    private long _position;
    private bool _disposed = false;

    /// <summary>
    /// 是否可读
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// 是否可写
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    /// 是否可寻址
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// 流长度
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// 当前读写位置
    /// </summary>
    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _position = value;
        }
    }

    /// <summary>
    /// 使用已有字节数据创建流
    /// </summary>
    /// <param name="data">初始字节数据</param>
    public VirtualFileStream(byte[] data)
    {
        _buffer = new byte[data.Length];
        Array.Copy(data, _buffer, data.Length);
        _length = data.Length;
        _position = 0;
    }

    /// <summary>
    /// 使用指定容量创建空流
    /// </summary>
    /// <param name="capacity">初始容量（字节）</param>
    public VirtualFileStream(int capacity)
    {
        _buffer = new byte[capacity];
        _length = 0;
        _position = 0;
    }

    /// <summary>
    /// 从当前位置读取字节到缓冲区
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">读取字节数</param>
    /// <returns>实际读取的字节数</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var remaining = _length - _position;
        if (remaining <= 0)
        {
            return 0;
        }

        var bytesToRead = (int)System.Math.Min(count, remaining);
        Array.Copy(_buffer, _position, buffer, offset, bytesToRead);
        _position += bytesToRead;

        return bytesToRead;
    }

    /// <summary>
    /// 将字节写入当前位置，自动扩展缓冲区
    /// </summary>
    /// <param name="buffer">源缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">写入字节数</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var endPosition = _position + count;
        if (endPosition > _buffer.Length)
        {
            EnsureCapacity((int)endPosition);
        }

        Array.Copy(buffer, offset, _buffer, _position, count);

        if (endPosition > _length)
        {
            _length = (int)endPosition;
        }

        _position = endPosition;
    }

    /// <summary>
    /// 设置读写位置
    /// </summary>
    /// <param name="offset">偏移量</param>
    /// <param name="origin">起始位置</param>
    /// <returns>新的读写位置</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), "无效的寻址起始位置")
        };

        ArgumentOutOfRangeException.ThrowIfNegative(newPosition);
        _position = newPosition;

        return _position;
    }

    /// <summary>
    /// 设置流长度
    /// </summary>
    /// <param name="value">新长度</param>
    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        if (value > _buffer.Length)
        {
            EnsureCapacity((int)value);
        }

        _length = (int)value;

        if (_position > _length)
        {
            _position = _length;
        }
    }

    /// <summary>
    /// 刷新流（内存流无操作）
    /// </summary>
    public override void Flush()
    {
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_buffer.Length >= requiredCapacity)
        {
            return;
        }

        var newCapacity = System.Math.Max(_buffer.Length * 2, requiredCapacity);
        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, _length);
        _buffer = newBuffer;
    }
}
