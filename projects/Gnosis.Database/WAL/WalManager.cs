using System.Buffers.Binary;
using System.IO;
using Gnosis.Core.Hash;
using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public sealed class WalManager : IWriteAheadLog
{
    #region 常量

    private const uint WalMagic = 0x57414C47;
    private const uint WalVersion = 1;
    private const int HeaderSize = 4096;
    private const int EntryHeaderSize = 29;

    #endregion

    #region 字段

    private readonly WalOptions _options;
    private FileStream? _fileStream;
    private BinaryWriter? _writer;
    private BinaryReader? _reader;
    private SequenceNumber _currentSequence;
    private WalCheckpoint? _latestCheckpoint;
    private long _currentFileSize;
    private int _currentFileIndex;
    private bool _disposed;

    #endregion

    #region 构造函数

    public WalManager(WalOptions options)
    {
        _options = options;
        _currentSequence = SequenceNumber.Zero;
        _currentFileIndex = 0;
        _currentFileSize = 0;
    }

    #endregion

    #region 属性

    public WalOptions Options => _options;

    #endregion

    #region 公开方法

    public async ValueTask<SequenceNumber> AppendAsync(WalEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_fileStream is null)
        {
            await EnsureFileOpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var actualChecksum = entry.ComputeChecksum();
        var entryWithChecksum = entry with { Checksum = actualChecksum };

        var serializedSize = EntryHeaderSize + entry.Key.Length + entry.Value.Length;

        if (_currentFileSize + serializedSize > _options.MaxFileSize)
        {
            await RollToNewFileAsync(cancellationToken).ConfigureAwait(false);
        }

        WriteEntry(entryWithChecksum);
        _currentSequence = _currentSequence.Next;

        if (_options.SyncOnCommit && entry.EntryType == WalEntryType.Commit)
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        return entryWithChecksum.Sequence;
    }

    public async ValueTask<IReadOnlyList<WalEntry>> ReadFromAsync(SequenceNumber sequence, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entries = new List<WalEntry>();

        var walFiles = GetWalFiles();
        if (walFiles.Length == 0)
        {
            return entries;
        }

        foreach (var walFile in walFiles)
        {
            await using var fs = new FileStream(walFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            using var br = new BinaryReader(fs);

            fs.Position = HeaderSize;

            while (fs.Position < fs.Length)
            {
                var entry = ReadEntry(br);
                if (entry is null)
                {
                    break;
                }

                if (entry.Value.Sequence.Value >= sequence.Value)
                {
                    var computedChecksum = entry.Value.ComputeChecksum();
                    if (computedChecksum != entry.Value.Checksum)
                    {
                        break;
                    }

                    entries.Add(entry.Value);
                }
            }
        }

        return entries;
    }

    public async ValueTask TruncateAsync(SequenceNumber sequence, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_fileStream is not null)
        {
            _writer?.Dispose();
            _reader?.Dispose();
            await _fileStream.DisposeAsync().ConfigureAwait(false);
            _fileStream = null;
            _writer = null;
            _reader = null;
        }

        var walFiles = GetWalFiles();
        foreach (var walFile in walFiles)
        {
            var tempFile = walFile + ".tmp";
            int copiedCount;

            await using (var sourceStream = new FileStream(walFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous))
            await using (var destStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                using var br = new BinaryReader(sourceStream);
                using var bw = new BinaryWriter(destStream);

                WriteHeader(bw);

                sourceStream.Position = HeaderSize;
                copiedCount = 0;

                while (sourceStream.Position < sourceStream.Length)
                {
                    var entry = ReadEntry(br);
                    if (entry is null)
                    {
                        break;
                    }

                    if (entry.Value.Sequence.Value >= sequence.Value)
                    {
                        WriteEntryTo(bw, entry.Value);
                        copiedCount++;
                    }
                }

                await destStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Delete(walFile);
            File.Move(tempFile, walFile);

            if (copiedCount == 0)
            {
                File.Delete(walFile);
            }
        }
    }

    public async ValueTask CheckpointAsync(WalCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _latestCheckpoint = checkpoint;

        if (_fileStream is null)
        {
            await EnsureFileOpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var checkpointEntry = new WalEntry(
            checkpoint.Sequence,
            TransactionId.Min,
            WalEntryType.Checkpoint,
            DatabaseKey.Empty,
            DatabaseValue.Empty,
            0);

        var actualChecksum = checkpointEntry.ComputeChecksum();
        checkpointEntry = checkpointEntry with { Checksum = actualChecksum };

        WriteEntry(checkpointEntry);
        await FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WalCheckpoint?> GetLatestCheckpointAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_latestCheckpoint is not null)
        {
            return _latestCheckpoint;
        }

        var walFiles = GetWalFiles();
        WalCheckpoint? latest = null;

        foreach (var walFile in walFiles)
        {
            await using var fs = new FileStream(walFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            using var br = new BinaryReader(fs);

            fs.Position = HeaderSize;

            while (fs.Position < fs.Length)
            {
                var entry = ReadEntry(br);
                if (entry is null)
                {
                    break;
                }

                if (entry.Value.EntryType == WalEntryType.Checkpoint)
                {
                    latest = new WalCheckpoint(
                        entry.Value.Sequence,
                        Timestamp.Now,
                        0,
                        0);
                }
            }
        }

        _latestCheckpoint = latest;
        return latest;
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_writer is not null)
        {
            _writer.Flush();
        }

        if (_fileStream is not null)
        {
            await _fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region 私有方法

    private async ValueTask EnsureFileOpenAsync(CancellationToken cancellationToken)
    {
        var dir = _options.Directory;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var walFiles = GetWalFiles();
        if (walFiles.Length > 0)
        {
            var lastFile = walFiles[^1];
            _currentFileIndex = ExtractFileIndex(lastFile);
            _fileStream = new FileStream(lastFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, _options.BufferSize, FileOptions.Asynchronous);
            _currentFileSize = _fileStream.Length;

            if (_currentFileSize < HeaderSize)
            {
                _writer = new BinaryWriter(_fileStream);
                WriteHeader(_writer);
                _currentFileSize = _fileStream.Position;
            }
            else
            {
                _writer = new BinaryWriter(_fileStream);
                await RecoverSequenceAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var newFile = Path.Combine(dir, $"wal_{_currentFileIndex:D6}.log");
            _fileStream = new FileStream(newFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, _options.BufferSize, FileOptions.Asynchronous);
            _writer = new BinaryWriter(_fileStream);
            WriteHeader(_writer);
            _currentFileSize = _fileStream.Position;
        }

        _reader = new BinaryReader(_fileStream);
    }

    private async ValueTask RecoverSequenceAsync(CancellationToken cancellationToken)
    {
        if (_fileStream is null)
        {
            return;
        }

        var readStream = new FileStream(_fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
        try
        {
            using var br = new BinaryReader(readStream);
            readStream.Position = HeaderSize;

            while (readStream.Position < readStream.Length)
            {
                var entry = ReadEntry(br);
                if (entry is null)
                {
                    break;
                }

                if (entry.Value.Sequence.Value >= _currentSequence.Value)
                {
                    _currentSequence = entry.Value.Sequence.Next;
                }

                if (entry.Value.EntryType == WalEntryType.Checkpoint)
                {
                    _latestCheckpoint = new WalCheckpoint(
                        entry.Value.Sequence,
                        Timestamp.Now,
                        0,
                        0);
                }
            }
        }
        finally
        {
            await readStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void WriteHeader(BinaryWriter writer)
    {
        writer.Write(WalMagic);
        writer.Write(WalVersion);
        writer.Write(_currentSequence.Value);
        writer.Write(new byte[HeaderSize - 16]);
        writer.Flush();
    }

    private void WriteEntry(WalEntry entry)
    {
        if (_writer is null)
        {
            return;
        }

        WriteEntryTo(_writer, entry);
        _currentFileSize += EntryHeaderSize + entry.Key.Length + entry.Value.Length;
    }

    private static void WriteEntryTo(BinaryWriter writer, WalEntry entry)
    {
        writer.Write(entry.Sequence.Value);
        writer.Write(entry.TransactionId.Value);
        writer.Write((byte)entry.EntryType);
        writer.Write(entry.Key.Length);
        writer.Write(entry.Value.Length);
        writer.Write(entry.Key.Bytes.Span);
        writer.Write(entry.Value.Bytes.Span);
        writer.Write(entry.Checksum);
    }

    private static WalEntry? ReadEntry(BinaryReader reader)
    {
        try
        {
            if (reader.BaseStream.Position + EntryHeaderSize > reader.BaseStream.Length)
            {
                return null;
            }

            var sequence = reader.ReadUInt64();
            var transactionId = reader.ReadUInt64();
            var entryType = reader.ReadByte();
            var keyLength = reader.ReadInt32();
            var valueLength = reader.ReadInt32();

            if (keyLength < 0 || valueLength < 0 || keyLength > 16 * 1024 * 1024 || valueLength > 16 * 1024 * 1024)
            {
                return null;
            }

            var remainingPayload = keyLength + valueLength + 4;
            if (reader.BaseStream.Position + remainingPayload > reader.BaseStream.Length)
            {
                return null;
            }

            var keyBytes = keyLength > 0 ? reader.ReadBytes(keyLength) : [];
            var valueBytes = valueLength > 0 ? reader.ReadBytes(valueLength) : [];
            var checksum = reader.ReadUInt32();

            var entry = new WalEntry(
                new SequenceNumber(sequence),
                new TransactionId(transactionId),
                (WalEntryType)entryType,
                new DatabaseKey(keyBytes),
                new DatabaseValue(valueBytes),
                checksum);

            var computedChecksum = entry.ComputeChecksum();
            if (computedChecksum != checksum)
            {
                return null;
            }

            return entry;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }

    private async ValueTask RollToNewFileAsync(CancellationToken cancellationToken)
    {
        if (_writer is not null)
        {
            _writer.Flush();
        }

        if (_fileStream is not null)
        {
            await _fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            await _fileStream.DisposeAsync().ConfigureAwait(false);
        }

        _currentFileIndex++;
        var newFile = Path.Combine(_options.Directory, $"wal_{_currentFileIndex:D6}.log");
        _fileStream = new FileStream(newFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, _options.BufferSize, FileOptions.Asynchronous);
        _writer = new BinaryWriter(_fileStream);
        _reader = new BinaryReader(_fileStream);
        WriteHeader(_writer);
        _currentFileSize = _fileStream.Position;
    }

    private string[] GetWalFiles()
    {
        var dir = _options.Directory;
        if (!Directory.Exists(dir))
        {
            return [];
        }

        return Directory.GetFiles(dir, "wal_*.log")
            .OrderBy(f => f)
            .ToArray();
    }

    private static int ExtractFileIndex(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var underscoreIndex = fileName.IndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex + 1 >= fileName.Length)
        {
            return 0;
        }

        var indexStr = fileName[(underscoreIndex + 1)..];
        return int.TryParse(indexStr, out var index) ? index : 0;
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

        if (_writer is not null)
        {
            _writer.Flush();
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
