using Gnosis.Database.Core;
using Gnosis.Database.Engine;
using Gnosis.Database.SHM;
using Gnosis.Database.Storage;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class GenesisKvDatabaseSyncTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_sync_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
        }
    }

    private GenesisKvDatabase CreateDatabase(bool syncOnCommit)
    {
        var options = new DatabaseOptions(
            Path: _testDir,
            Storage: new StorageOptions(
                EngineType: StorageEngineType.MemoryMappedFile,
                BasePath: Path.Combine(_testDir, "data.db"),
                PageSize: 4096,
                InitialSize: 16 * 1024 * 1024,
                MaxSize: long.MaxValue,
                UseDirectIO: false,
                UseSparseFile: true),
            Wal: new WalOptions(
                Directory: Path.Combine(_testDir, "wal"),
                MaxFileSize: 64 * 1024 * 1024,
                SyncOnCommit: syncOnCommit,
                CompressionEnabled: false,
                BufferSize: 4096),
            Shm: new ShmOptions(
                Name: "sync_shm",
                MaxSize: 64 * 1024 * 1024,
                PageSize: 4096,
                MaxPageCount: 16384,
                EvictionThreshold: 0.85,
                EnableCrossProcess: false),
            BTreeOrder: 128,
            ReadOnly: false,
            AutoCheckpoint: true,
            CheckpointIntervalMs: 60000);

        return new GenesisKvDatabase(options);
    }

    [Test]
    public async Task PutAsync_SyncOnCommitTrue_1000Keys_GetAsync_Works()
    {
        using var db = CreateDatabase(syncOnCommit: true);

        for (var i = 0; i < 1000; i++)
        {
            var key = DatabaseKey.FromString($"sync:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < 1000; i += 100)
        {
            var key = DatabaseKey.FromString($"sync:{i:D8}");
            var result = await db.GetAsync(key);
            TestContext.WriteLine($"Get sync:{i:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key sync:{i:D8} should exist");
        }
    }

    [Test]
    public async Task PutAsync_SyncOnCommitFalse_1000Keys_GetAsync_Works()
    {
        using var db = CreateDatabase(syncOnCommit: false);

        for (var i = 0; i < 1000; i++)
        {
            var key = DatabaseKey.FromString($"nosync:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < 1000; i += 100)
        {
            var key = DatabaseKey.FromString($"nosync:{i:D8}");
            var result = await db.GetAsync(key);
            TestContext.WriteLine($"Get nosync:{i:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key nosync:{i:D8} should exist");
        }
    }
}
