using Gnosis.Database;
using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Tests.Database;

[TestFixture]
public class BTreeDiagnosticTests
{
    private BTreeIndex _tree = null!;
    private const int Order = 4;

    [SetUp]
    public void SetUp()
    {
        _tree = new BTreeIndex(Order);
    }

    [TearDown]
    public void TearDown()
    {
        _tree.Dispose();
    }

    [Test]
    public async Task InsertAsync_SequentialKeys_AllFound()
    {
        for (var i = 0; i < 30; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));

            for (var j = 0; j <= i; j++)
            {
                var result = await _tree.SearchAsync(DatabaseKey.FromUInt64((ulong)j));
                Assert.That(result, Is.Not.Null, $"Key {j} not found after inserting key {i}. Tree count: {_tree.Count}, Height: {_tree.Height}");
            }
        }
    }

    [Test]
    public async Task InsertAsync_LargerOrder_NoSplit()
    {
        var tree = new BTreeIndex(128);
        for (var i = 0; i < 100; i++)
        {
            await tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < 100; i++)
        {
            var result = await tree.SearchAsync(DatabaseKey.FromUInt64((ulong)i));
            Assert.That(result, Is.Not.Null, $"Key {i} not found with order 128");
        }

        tree.Dispose();
    }

    [Test]
    public async Task InsertAsync_SmallOrder_ManySplits()
    {
        var tree = new BTreeIndex(3);
        for (var i = 0; i < 50; i++)
        {
            await tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < 50; i++)
        {
            var result = await tree.SearchAsync(DatabaseKey.FromUInt64((ulong)i));
            Assert.That(result, Is.Not.Null, $"Key {i} not found with order 3");
        }

        tree.Dispose();
    }
}
