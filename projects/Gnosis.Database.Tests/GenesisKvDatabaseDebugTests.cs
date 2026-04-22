using Gnosis.Database.Core;
using Gnosis.Database.Engine;
using Gnosis.Database.SHM;
using Gnosis.Database.Storage;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class GenesisKvDatabaseDebugTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_debug_{Guid.NewGuid()}");
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

    private GenesisKvDatabase CreateDatabase()
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
                MaxFileSize: 256 * 1024 * 1024,
                SyncOnCommit: false,
                CompressionEnabled: false,
                BufferSize: 65536),
            Shm: new ShmOptions(
                Name: "debug_shm",
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
    public async Task Seek_AfterManyWrites_Works()
    {
        using var db = CreateDatabase();
        const int count = 50000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        var startKey = DatabaseKey.FromString("range:01000000");
        var result = await db.GetAsync(startKey);

        TestContext.WriteLine($"GetAsync result: {result is not null}");
        if (result is not null)
        {
            TestContext.WriteLine($"GetAsync value: {System.Text.Encoding.UTF8.GetString(result.Value.Bytes.Span)}");
        }

        using var cursor = db.Seek(startKey);
        TestContext.WriteLine($"Seek IsValid: {cursor.IsValid}");

        if (cursor.IsValid)
        {
            TestContext.WriteLine($"Seek Current: {System.Text.Encoding.UTF8.GetString(cursor.Current.Key.Bytes.Span)}");
        }

        var firstKey = DatabaseKey.FromString("range:00000000");
        var firstResult = await db.GetAsync(firstKey);
        TestContext.WriteLine($"First key result: {firstResult is not null}");

        var lastKey = DatabaseKey.FromString("range:00000010");
        var lastResult = await db.GetAsync(lastKey);
        TestContext.WriteLine($"Last key result: {lastResult is not null}");

        var midKey = DatabaseKey.FromString("range:00000050");
        var midResult = await db.GetAsync(midKey);
        TestContext.WriteLine($"Mid key result: {midResult is not null}");

        var manyKey = DatabaseKey.FromString("range:00001000");
        var manyResult = await db.GetAsync(manyKey);
        TestContext.WriteLine($"Many key result: {manyResult is not null}");

        // 使用反射检查 BTreeIndex 的 Count
        var btreeField = typeof(GenesisKvDatabase).GetField("_btree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var btree = btreeField?.GetValue(db);
        var countProperty = btree?.GetType().GetProperty("Count");
        var btreeCount = countProperty?.GetValue(btree);
        TestContext.WriteLine($"BTreeIndex.Count: {btreeCount}");

        Assert.That(result, Is.Not.Null);
    }
}
