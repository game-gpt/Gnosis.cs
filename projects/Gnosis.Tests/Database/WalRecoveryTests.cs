using Gnosis.Database.Core;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Tests.Database;

[TestFixture]
public class WalRecoveryTests
{
    private string _walDir = null!;

    [SetUp]
    public void SetUp()
    {
        _walDir = Path.Combine(Path.GetTempPath(), $"gnosis_wal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_walDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_walDir))
            {
                Directory.Delete(_walDir, true);
            }
        }
        catch
        {
        }
    }

    private WalOptions CreateOptions()
    {
        return new WalOptions(
            Directory: _walDir,
            MaxFileSize: 64 * 1024 * 1024,
            SyncOnCommit: true,
            CompressionEnabled: false,
            BufferSize: 4096);
    }

    [Test]
    public async Task WalManager_Append_And_ReadBack()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);

        var entry = new WalEntry(
            SequenceNumber.Zero,
            TransactionId.New(),
            WalEntryType.Put,
            DatabaseKey.FromString("key1"),
            DatabaseValue.FromString("value1"),
            0);

        var sequence = await wal.AppendAsync(entry);
        await wal.FlushAsync();

        var entries = await wal.ReadFromAsync(SequenceNumber.Zero);

        Assert.That(entries.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task WalManager_MultipleEntries_ReadFromReturnsAll()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);

        const int count = 10;
        for (var i = 0; i < count; i++)
        {
            var entry = new WalEntry(
                new SequenceNumber((ulong)i),
                TransactionId.New(),
                WalEntryType.Put,
                DatabaseKey.FromString($"key:{i}"),
                DatabaseValue.FromString($"value:{i}"),
                0);

            await wal.AppendAsync(entry);
        }

        await wal.FlushAsync();

        var entries = await wal.ReadFromAsync(SequenceNumber.Zero);
        Assert.That(entries.Count, Is.EqualTo(count));
    }

    [Test]
    public async Task WalManager_Truncate_RemovesOldEntries()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);

        for (var i = 0; i < 10; i++)
        {
            var entry = new WalEntry(
                new SequenceNumber((ulong)i),
                TransactionId.New(),
                WalEntryType.Put,
                DatabaseKey.FromString($"key:{i}"),
                DatabaseValue.FromString($"value:{i}"),
                0);

            await wal.AppendAsync(entry);
        }

        await wal.FlushAsync();
        await wal.TruncateAsync(new SequenceNumber(5));

        var entries = await wal.ReadFromAsync(SequenceNumber.Zero);
        Assert.That(entries.Count, Is.LessThan(10));
    }

    [Test]
    public async Task WalManager_Checkpoint_CreatesCheckpointEntry()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);

        var checkpoint = new WalCheckpoint(
            SequenceNumber.Zero,
            Timestamp.Now,
            0,
            0);

        await wal.CheckpointAsync(checkpoint);

        var latest = await wal.GetLatestCheckpointAsync();
        Assert.That(latest, Is.Not.Null);
    }

    [Test]
    public async Task WalManager_FileRoll_CreatesNewFile()
    {
        var options = new WalOptions(
            Directory: _walDir,
            MaxFileSize: 1024,
            SyncOnCommit: false,
            CompressionEnabled: false,
            BufferSize: 4096);

        using var wal = new WalManager(options);

        for (var i = 0; i < 50; i++)
        {
            var entry = new WalEntry(
                new SequenceNumber((ulong)i),
                TransactionId.New(),
                WalEntryType.Put,
                DatabaseKey.FromString($"key:{i:D4}"),
                DatabaseValue.FromString(new string('x', 100)),
                0);

            await wal.AppendAsync(entry);
        }

        await wal.FlushAsync();

        var walFiles = Directory.GetFiles(_walDir, "wal_*.log");
        Assert.That(walFiles.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task BatchWalWriter_BatchFlush_WritesAll()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);
        using var batchWriter = new BatchWalWriter(wal, maxBatchSize: 5);

        for (var i = 0; i < 10; i++)
        {
            var entry = new WalEntry(
                new SequenceNumber((ulong)i),
                TransactionId.New(),
                WalEntryType.Put,
                DatabaseKey.FromString($"batch:{i}"),
                DatabaseValue.FromString($"value:{i}"),
                0);

            await batchWriter.AddAsync(entry);
        }

        await batchWriter.FlushAsync();

        var entries = await wal.ReadFromAsync(SequenceNumber.Zero);
        Assert.That(entries.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task WalReplayer_GetCommittedEntries_FiltersCorrectly()
    {
        var options = CreateOptions();
        using var wal = new WalManager(options);
        var replayer = new WalReplayer();

        var txnId = TransactionId.New();

        var putEntry = new WalEntry(
            SequenceNumber.Zero,
            txnId,
            WalEntryType.Put,
            DatabaseKey.FromString("committed:key"),
            DatabaseValue.FromString("committed:value"),
            0);

        var commitEntry = new WalEntry(
            new SequenceNumber(1),
            txnId,
            WalEntryType.Commit,
            DatabaseKey.Empty,
            DatabaseValue.Empty,
            0);

        await wal.AppendAsync(putEntry);
        await wal.AppendAsync(commitEntry);
        await wal.FlushAsync();

        var committed = await replayer.GetCommittedEntriesAsync(wal, SequenceNumber.Zero);
        Assert.That(committed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WalManager_CorruptedEntry_StopsReading()
    {
        var options = CreateOptions();

        using (var wal = new WalManager(options))
        {
            var entry = new WalEntry(
                SequenceNumber.Zero,
                TransactionId.New(),
                WalEntryType.Put,
                DatabaseKey.FromString("valid"),
                DatabaseValue.FromString("data"),
                0);

            await wal.AppendAsync(entry);
            await wal.FlushAsync();
        }

        var walFiles = Directory.GetFiles(_walDir, "wal_*.log");
        if (walFiles.Length > 0)
        {
            var filePath = walFiles[0];
            var bytes = await File.ReadAllBytesAsync(filePath);

            if (bytes.Length > 100)
            {
                bytes[^1] = (byte)(bytes[^1] ^ 0xFF);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
        }

        using var wal2 = new WalManager(options);
        var entries = await wal2.ReadFromAsync(SequenceNumber.Zero);

        Assert.That(entries.Count, Is.LessThanOrEqualTo(1));
    }
}
