using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using Gnosis.Database.Transaction;
using Gnosis.Database.WAL;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class TransactionManagerBTreeTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"gnosis_txn_btree_{Guid.NewGuid()}");
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

    [Test]
    public async Task TransactionManager_PutAsync_1000Keys_AllSearchable()
    {
        using var btree = new BTreeIndex(128);
        var walOptions = new WalOptions(
            Directory: Path.Combine(_testDir, "wal"),
            MaxFileSize: 64 * 1024 * 1024,
            SyncOnCommit: false,
            CompressionEnabled: false,
            BufferSize: 4096);
        using var wal = new WalManager(walOptions);
        var txnManager = new TransactionManager(btree, wal);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            using var txn = txnManager.BeginTransaction();
            var key = DatabaseKey.FromString($"txn:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await txn.PutAsync(key, value);
            await txn.CommitAsync();
        }

        TestContext.WriteLine($"BTree Count: {btree.Count}, Height: {btree.Height}");

        for (var i = 0; i < count; i += 100)
        {
            var key = DatabaseKey.FromString($"txn:{i:D8}");
            var result = await btree.SearchAsync(key);
            TestContext.WriteLine($"Search txn:{i:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key txn:{i:D8} should exist");
        }
    }

    [Test]
    public async Task DirectBTree_1000Keys_AllSearchable()
    {
        using var btree = new BTreeIndex(128);
        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"direct:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await btree.InsertAsync(key, value);
        }

        TestContext.WriteLine($"BTree Count: {btree.Count}, Height: {btree.Height}");

        for (var i = 0; i < count; i += 100)
        {
            var key = DatabaseKey.FromString($"direct:{i:D8}");
            var result = await btree.SearchAsync(key);
            TestContext.WriteLine($"Search direct:{i:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key direct:{i:D8} should exist");
        }
    }

    [Test]
    public async Task TransactionManager_WithWal_PutAsync_100Keys_AllSearchable()
    {
        using var btree = new BTreeIndex(128);
        var walOptions = new WalOptions(
            Directory: Path.Combine(_testDir, "wal"),
            MaxFileSize: 64 * 1024 * 1024,
            SyncOnCommit: true,
            CompressionEnabled: false,
            BufferSize: 4096);
        using var wal = new WalManager(walOptions);
        var txnManager = new TransactionManager(btree, wal);

        const int count = 100;

        for (var i = 0; i < count; i++)
        {
            using var txn = txnManager.BeginTransaction();
            var key = DatabaseKey.FromString($"waltxn:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await txn.PutAsync(key, value);
            await txn.CommitAsync();
        }

        TestContext.WriteLine($"BTree Count: {btree.Count}, Height: {btree.Height}");

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"waltxn:{i:D8}");
            var result = await btree.SearchAsync(key);
            if (result is null)
            {
                TestContext.WriteLine($"FAILED: Search waltxn:{i:D8} = {result is not null}");
            }
        }

        // 验证所有 key
        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"waltxn:{i:D8}");
            var result = await btree.SearchAsync(key);
            Assert.That(result, Is.Not.Null, $"Key waltxn:{i:D8} should exist");
        }
    }
}
