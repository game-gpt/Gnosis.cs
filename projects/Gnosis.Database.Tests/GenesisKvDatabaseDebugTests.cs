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
                MaxFileSize: 64 * 1024 * 1024,
                SyncOnCommit: true,
                CompressionEnabled: false,
                BufferSize: 4096),
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

        var startKey = DatabaseKey.FromString("range:00010000");
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

        var earlyKey = DatabaseKey.FromString("range:00000010");
        var earlyResult = await db.GetAsync(earlyKey);
        TestContext.WriteLine($"Early key result: {earlyResult is not null}");

        var midKey = DatabaseKey.FromString("range:00002500");
        var midResult = await db.GetAsync(midKey);
        TestContext.WriteLine($"Mid key result: {midResult is not null}");

        var manyKey = DatabaseKey.FromString("range:00004999");
        var manyResult = await db.GetAsync(manyKey);
        TestContext.WriteLine($"Last key result: {manyResult is not null}");

        // 直接测试 BTreeIndex
        var btreeField = typeof(GenesisKvDatabase).GetField("_btree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var btree = btreeField?.GetValue(db);
        TestContext.WriteLine($"BTree type: {btree?.GetType().FullName}");

        var countProperty = btree?.GetType().GetProperty("Count");
        var btreeCount = countProperty?.GetValue(btree);
        var heightProperty = btree?.GetType().GetProperty("Height");
        var btreeHeight = heightProperty?.GetValue(btree);
        TestContext.WriteLine($"BTreeIndex.Count: {btreeCount}, Height: {btreeHeight}");

        // 使用 BTreeIndex 的 SearchAsync 直接搜索
        var searchMethod = btree?.GetType().GetMethod("SearchAsync", new[] { typeof(DatabaseKey), typeof(CancellationToken) });
        var searchResultObj = searchMethod?.Invoke(btree, new object[] { startKey, default(CancellationToken) });
        TestContext.WriteLine($"SearchAsync returned: {searchResultObj?.GetType().FullName}");

        if (searchResultObj is not null)
        {
            var asTaskMethod = searchResultObj.GetType().GetMethod("AsTask");
            if (asTaskMethod is not null)
            {
                var task = asTaskMethod.Invoke(searchResultObj, null) as System.Threading.Tasks.Task;
                if (task is not null)
                {
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    var searchResult = resultProperty?.GetValue(task);
                    TestContext.WriteLine($"Direct SearchAsync result: {searchResult is not null}");
                }
            }
        }

        Assert.That(result, Is.Not.Null);
    }
}
