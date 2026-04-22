using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class BTreeInsertComparisonTests
{
    [Test]
    public async Task DirectInsert_vs_TransactionInsert_SameKeys()
    {
        using var directTree = new BTreeIndex(128);
        using var txnTree = new BTreeIndex(128);

        const int count = 1000;

        // 直接插入
        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"test:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await directTree.InsertAsync(key, value);
        }

        // 模拟事务方式插入（直接调用相同的 InsertAsync）
        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"test:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await txnTree.InsertAsync(key, value);
        }

        TestContext.WriteLine($"Direct tree - Count: {directTree.Count}, Height: {directTree.Height}");
        TestContext.WriteLine($"Txn tree - Count: {txnTree.Count}, Height: {txnTree.Height}");

        // 验证两个树都能搜索到相同的 key
        for (var i = 0; i < count; i += 100)
        {
            var key = DatabaseKey.FromString($"test:{i:D8}");
            var directResult = await directTree.SearchAsync(key);
            var txnResult = await txnTree.SearchAsync(key);

            Assert.That(directResult, Is.Not.Null, $"Direct tree should find key test:{i:D8}");
            Assert.That(txnResult, Is.Not.Null, $"Txn tree should find key test:{i:D8}");
        }
    }

    [Test]
    public async Task DirectInsert_10000Keys_AllSearchable()
    {
        using var tree = new BTreeIndex(128);
        const int count = 10000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await tree.InsertAsync(key, value);
        }

        TestContext.WriteLine($"Count: {tree.Count}, Height: {tree.Height}");

        // 搜索几个关键 key
        var testKeys = new[] { 0, 1, 10, 100, 1000, 5000, 9999 };
        foreach (var idx in testKeys)
        {
            var key = DatabaseKey.FromString($"range:{idx:D8}");
            var result = await tree.SearchAsync(key);
            TestContext.WriteLine($"Search range:{idx:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key range:{idx:D8} should exist");
        }
    }
}
