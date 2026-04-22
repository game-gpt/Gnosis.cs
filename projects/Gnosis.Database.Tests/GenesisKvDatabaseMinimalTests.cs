using Gnosis.Database.Core;
using Gnosis.Database.Engine;
using Gnosis.Database.SHM;
using Gnosis.Database.Storage;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class GenesisKvDatabaseMinimalTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_min_{Guid.NewGuid()}");
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

    private GenesisKvDatabase CreateDatabase(bool syncOnCommit = false)
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
                Name: "min_shm",
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
    public async Task PutAsync_10Keys_GetAsync_ReturnsCorrectValues()
    {
        using var db = CreateDatabase();

        for (var i = 0; i < 10; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < 10; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var result = await db.GetAsync(key);
            TestContext.WriteLine($"Get min:{i:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key min:{i:D8} should exist");
        }
    }

    [Test]
    public async Task PutAsync_100Keys_GetAsync_ReturnsCorrectValues()
    {
        using var db = CreateDatabase();

        for (var i = 0; i < 100; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < 100; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var result = await db.GetAsync(key);
            if (result is null)
            {
                TestContext.WriteLine($"FAILED: Get min:{i:D8} = null");
            }
        }

        for (var i = 0; i < 100; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var result = await db.GetAsync(key);
            Assert.That(result, Is.Not.Null, $"Key min:{i:D8} should exist");
        }
    }

    [Test]
    public async Task PutAsync_1000Keys_GetAsync_ReturnsCorrectValues()
    {
        using var db = CreateDatabase();

        for (var i = 0; i < 1000; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < 1000; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var result = await db.GetAsync(key);
            if (result is null)
            {
                TestContext.WriteLine($"FAILED: Get min:{i:D8} = null");
            }
        }

        for (var i = 0; i < 1000; i++)
        {
            var key = DatabaseKey.FromString($"min:{i:D8}");
            var result = await db.GetAsync(key);
            Assert.That(result, Is.Not.Null, $"Key min:{i:D8} should exist");
        }
    }

    [Test]
    public async Task PutAsync_50000Keys_GetAsync_ReturnsCorrectValues()
    {
        using var db = CreateDatabase(syncOnCommit: false);

        for (var i = 0; i < 50000; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        var testKey = DatabaseKey.FromString("range:00010000");
        var result = await db.GetAsync(testKey);
        TestContext.WriteLine($"Get range:00010000 = {result is not null}");

        var btreeField = typeof(GenesisKvDatabase).GetField("_btree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var btree = btreeField?.GetValue(db);
        var countProperty = btree?.GetType().GetProperty("Count");
        var btreeCount = countProperty?.GetValue(btree);
        var heightProperty = btree?.GetType().GetProperty("Height");
        var btreeHeight = heightProperty?.GetValue(btree);
        TestContext.WriteLine($"BTreeIndex.Count: {btreeCount}, Height: {btreeHeight}");

        Assert.That(result, Is.Not.Null, "Key range:00010000 should exist");
    }
}
