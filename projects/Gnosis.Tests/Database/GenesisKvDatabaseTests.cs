using Gnosis.Database.Core;
using Gnosis.Database.Engine;
using Gnosis.Database.Storage;
using Gnosis.Database.SHM;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Tests.Database;

[TestFixture]
public class GenesisKvDatabaseTests
{
    private GenesisKvDatabase _db = null!;
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

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
                MaxFileSize: 64 * 1024 * 1024,
                SyncOnCommit: true,
                CompressionEnabled: false,
                BufferSize: 4096),
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

        _db = new GenesisKvDatabase(options);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();

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

    [Test]
    public async Task PutAsync_And_GetAsync_ReturnsCorrectValue()
    {
        var key = DatabaseKey.FromString("player:1");
        var value = DatabaseValue.FromString("Alice");

        await _db.PutAsync(key, value);
        var result = await _db.GetAsync(key);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(value));
    }

    [Test]
    public async Task GetAsync_NonExistingKey_ReturnsNull()
    {
        var result = await _db.GetAsync(DatabaseKey.FromString("nonexistent"));
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ExistingKey_RemovesValue()
    {
        var key = DatabaseKey.FromString("temp:key");
        await _db.PutAsync(key, DatabaseValue.FromString("temp"));

        var deleted = await _db.DeleteAsync(key);
        var result = await _db.GetAsync(key);

        Assert.That(deleted, Is.True);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task PutAsync_ManyKeys_AllRetrievable()
    {
        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"key:{i:D6}");
            var value = DatabaseValue.FromString($"value:{i}");
            await _db.PutAsync(key, value);
        }

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"key:{i:D6}");
            var result = await _db.GetAsync(key);
            Assert.That(result, Is.Not.Null, $"Key {i} not found");
        }
    }

    [Test]
    public async Task Transaction_Commit_PersistsData()
    {
        var txn = _db.BeginTransaction();
        var key = DatabaseKey.FromString("txn:key");
        var value = DatabaseValue.FromString("txn:value");

        await txn.PutAsync(key, value);
        await txn.CommitAsync();

        var result = await _db.GetAsync(key);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(value));
    }

    [Test]
    public async Task Transaction_Rollback_DiscardsData()
    {
        var txn = _db.BeginTransaction();
        var key = DatabaseKey.FromString("txn:rollback");

        await txn.PutAsync(key, DatabaseValue.FromString("should not persist"));
        txn.Rollback();

        var result = await _db.GetAsync(key);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Transaction_ReadYourOwnWrites()
    {
        var txn = _db.BeginTransaction();
        var key = DatabaseKey.FromString("txn:ryow");
        var value = DatabaseValue.FromString("written");

        await txn.PutAsync(key, value);
        var result = await txn.GetAsync(key);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(value));

        await txn.CommitAsync();
    }

    [Test]
    public void CreateSnapshot_ReturnsReadableSnapshot()
    {
        using var snapshot = _db.CreateSnapshot();
        Assert.That(snapshot, Is.Not.Null);
    }

    [Test]
    public async Task Snapshot_Isolation_ReadCommittedData()
    {
        var key = DatabaseKey.FromString("snapshot:key");
        var value = DatabaseValue.FromString("snapshot:value");
        await _db.PutAsync(key, value);

        using var snapshot = _db.CreateSnapshot();
        var result = await snapshot.GetAsync(key);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(value));
    }

    [Test]
    public async Task Cursor_Seek_TraversesInOrder()
    {
        for (var i = 10; i >= 1; i--)
        {
            await _db.PutAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _db.Seek(DatabaseKey.FromUInt64(1));
        var keys = new List<ulong>();

        while (cursor.IsValid)
        {
            keys.Add(BitConverter.ToUInt64(cursor.Current.Key.Bytes.Span));
            if (!cursor.MoveNext())
            {
                break;
            }
        }

        Assert.That(keys, Is.Ordered);
        Assert.That(keys.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task Statistics_AfterWrites_CountIncreases()
    {
        var initialWrites = _db.Statistics.TotalWrites;

        await _db.PutAsync(DatabaseKey.FromString("stat:key"), DatabaseValue.FromString("value"));

        Assert.That(_db.Statistics.TotalWrites, Is.GreaterThan(initialWrites));
    }
}
