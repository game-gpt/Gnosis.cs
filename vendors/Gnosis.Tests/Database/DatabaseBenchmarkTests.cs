using System.Diagnostics;
using Gnosis.Database.Core;
using Gnosis.Database.Engine;
using NUnit.Framework;

namespace Gnosis.Database;

[TestFixture]
[Category("Benchmark")]
public class DatabaseBenchmarkTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_bench_{Guid.NewGuid()}");
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
                BasePath: Path.Combine(_testDir, "data.db"),
                PageSize: 4096,
                EngineType: StorageEngineType.MemoryMappedFile,
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
                Name: "genesis_db_shm",
                MaxSize: 64 * 1024 * 1024,
                PageSize: 4096,
                MaxPageCount: 65536,
                EvictionThreshold: 0.85,
                EnableCrossProcess: false),
            BTreeOrder: 128,
            ReadOnly: false,
            AutoCheckpoint: true,
            CheckpointIntervalMs: 60000);

        return new GenesisKvDatabase(options);
    }

    [Test]
    public async Task Benchmark_WriteThroughput()
    {
        using var db = CreateDatabase();
        const int count = 100_000;

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"bench:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        stopwatch.Stop();

        var opsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
        TestContext.WriteLine($"写入吞吐量: {opsPerSecond:F0} ops/s ({stopwatch.ElapsedMilliseconds}ms for {count} ops)");

        Assert.That(opsPerSecond, Is.GreaterThan(1000), "写入吞吐量应大于 1000 ops/s");
    }

    [Test]
    public async Task Benchmark_ReadLatency()
    {
        using var db = CreateDatabase();
        const int count = 10_000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"bench:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"bench:{i:D8}");
            await db.GetAsync(key);
        }

        stopwatch.Stop();

        var avgLatencyUs = stopwatch.Elapsed.TotalMicroseconds / count;
        TestContext.WriteLine($"平均读取延迟: {avgLatencyUs:F2} μs ({stopwatch.ElapsedMilliseconds}ms for {count} reads)");

        Assert.That(avgLatencyUs, Is.LessThan(1000), "平均读取延迟应小于 1000 μs");
    }

    [Test]
    public async Task Benchmark_CacheHitLatency()
    {
        using var db = CreateDatabase();
        const int count = 10_000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"cache:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"cache:{i:D8}");
            await db.GetAsync(key);
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"cache:{i:D8}");
            await db.GetAsync(key);
        }

        stopwatch.Stop();

        var avgLatencyUs = stopwatch.Elapsed.TotalMicroseconds / count;
        TestContext.WriteLine($"缓存命中读取延迟: {avgLatencyUs:F2} μs ({stopwatch.ElapsedMilliseconds}ms for {count} cached reads)");

        Assert.That(avgLatencyUs, Is.LessThan(10), "缓存命中延迟应小于 10 μs");
    }

    [Test]
    public async Task Benchmark_BTreeSearchLatency()
    {
        using var db = CreateDatabase();
        const int count = 100_000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"btree:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"btree:{i:D8}");
            await db.GetAsync(key);
        }

        stopwatch.Stop();

        var avgLatencyUs = stopwatch.Elapsed.TotalMicroseconds / count;
        TestContext.WriteLine($"B+ 树搜索延迟: {avgLatencyUs:F2} μs ({stopwatch.ElapsedMilliseconds}ms for {count} searches)");

        Assert.That(avgLatencyUs, Is.LessThan(100), "B+ 树搜索延迟应小于 100 μs");
    }

    [Test]
    public async Task Benchmark_RangeScanThroughput()
    {
        using var db = CreateDatabase();
        const int count = 50_000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await db.PutAsync(key, value);
        }

        var startKey = DatabaseKey.FromString("range:01000000");
        var endKey = DatabaseKey.FromString("range:02000000");

        using var cursor = db.Seek(startKey);
        var stopwatch = Stopwatch.StartNew();

        var entries = cursor.GetRange(startKey, endKey, limit: 10000);

        stopwatch.Stop();

        var entriesPerSecond = entries.Count / stopwatch.Elapsed.TotalSeconds;
        TestContext.WriteLine($"范围扫描吞吐: {entriesPerSecond:F0} entries/s ({stopwatch.ElapsedMilliseconds}ms for {entries.Count} entries)");

        Assert.That(entriesPerSecond, Is.GreaterThan(1000), "范围扫描吞吐应大于 1000 entries/s");
    }

    [Test]
    public async Task Benchmark_TransactionThroughput()
    {
        using var db = CreateDatabase();
        const int count = 10_000;

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            using var txn = db.BeginTransaction();
            var key = DatabaseKey.FromString($"txn:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await txn.PutAsync(key, value);
            await txn.CommitAsync();
        }

        stopwatch.Stop();

        var txnsPerSecond = count / stopwatch.Elapsed.TotalSeconds;
        TestContext.WriteLine($"事务吞吐: {txnsPerSecond:F0} txns/s ({stopwatch.ElapsedMilliseconds}ms for {count} transactions)");

        Assert.That(txnsPerSecond, Is.GreaterThan(100), "事务吞吐应大于 100 txns/s");
    }
}
