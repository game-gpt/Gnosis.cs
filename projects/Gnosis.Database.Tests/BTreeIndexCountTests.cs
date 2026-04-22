using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class BTreeIndexCountTests
{
    [Test]
    public async Task InsertAsync_50000Keys_CountIsCorrect()
    {
        using var tree = new BTreeIndex(128);
        const int count = 50000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await tree.InsertAsync(key, value);
        }

        TestContext.WriteLine($"BTreeIndex.Count: {tree.Count}");
        Assert.That(tree.Count, Is.EqualTo(count));
    }

    [Test]
    public async Task InsertAsync_50000Keys_SearchReturnsCorrect()
    {
        using var tree = new BTreeIndex(128);
        const int count = 50000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await tree.InsertAsync(key, value);
        }

        var result = await tree.SearchAsync(DatabaseKey.FromString("range:00010000"));
        TestContext.WriteLine($"Search result: {result is not null}");
        Assert.That(result, Is.Not.Null);
    }
}
