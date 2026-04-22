using System.IO;
using Gnosis.Database.Core;
using Gnosis.Database.Storage;

namespace Gnosis.Database.Engine;

public sealed class PagedStorageEngine : IStorageEngine
{
    #region 常量

    private const uint EngineMagic = 0x474E4442;
    private const uint EngineVersion = 1;
    private const int HeaderSize = 4096;

    #endregion

    #region 字段

    private readonly StorageOptions _options;
    private FileStream? _fileStream;
    private BinaryWriter? _writer;
    private BinaryReader? _reader;
    private long _length;
    private bool _disposed;

    #endregion

    #region 构造函数

    public PagedStorageEngine(StorageOptions options)
    {
        _options = options;
        _length = 0;
    }

    #endregion

    #region 属性

    public StorageEngineType Type => _options.EngineType;

    public long Length => _length;

    #endregion

    #region 公开方法

    public async ValueTask InitializeAsync(StorageOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dir = Path.GetDirectoryName(options.BasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _fileStream = new FileStream(
            options.BasePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            options.PageSize,
            FileOptions.Asynchronous);

        _writer = new BinaryWriter(_fileStream);
        _reader = new BinaryReader(_fileStream);

        if (_fileStream.Length == 0)
        {
            WriteFileHeader();
        }
        else
        {
            await ReadFileHeaderAsync(cancellationToken).ConfigureAwait(false);
        }

        _length = _fileStream.Length;
    }

    public async ValueTask WriteAsync(long offset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_fileStream is null)
        {
            return;
        }

        _fileStream.Position = offset;
        await _fileStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);

        if (offset + data.Length > _length)
        {
            _length = offset + data.Length;
        }
    }

    public async ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_fileStream is null)
        {
            return 0;
        }

        _fileStream.Position = offset;
        return await _fileStream.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_fileStream is not null)
        {
            await _fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask CompactAsync(CompactionMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region 私有方法

    private void WriteFileHeader()
    {
        if (_writer is null)
        {
            return;
        }

        _writer.Write(EngineMagic);
        _writer.Write(EngineVersion);
        _writer.Write(_options.PageSize);
        _writer.Write(0L);
        _writer.Write(new byte[HeaderSize - 24]);
        _writer.Flush();
    }

    private async ValueTask ReadFileHeaderAsync(CancellationToken cancellationToken)
    {
        if (_fileStream is null || _reader is null)
        {
            return;
        }

        _fileStream.Position = 0;

        var headerBuffer = new byte[HeaderSize];
        await _fileStream.ReadAsync(headerBuffer, cancellationToken).ConfigureAwait(false);

        _fileStream.Position = _fileStream.Length;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _fileStream?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_fileStream is not null)
        {
            await _fileStream.FlushAsync().ConfigureAwait(false);
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _fileStream?.Dispose();
        _disposed = true;
    }

    #endregion
}
