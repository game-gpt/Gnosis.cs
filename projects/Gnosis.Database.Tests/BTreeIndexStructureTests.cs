using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class BTreeIndexStructureTests
{
    [Test]
    public async Task InsertAsync_50000Keys_StructureIsValid()
    {
        using var tree = new BTreeIndex(128);
        const int count = 50000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await tree.InsertAsync(key, value);
        }

        TestContext.WriteLine($"Height: {tree.Height}");
        TestContext.WriteLine($"Count: {tree.Count}");

        // 检查随机 key
        var random = new Random(42);
        for (var i = 0; i < 10; i++)
        {
            var idx = random.Next(count);
            var key = DatabaseKey.FromString($"range:{idx:D8}");
            var result = await tree.SearchAsync(key);
            TestContext.WriteLine($"Search range:{idx:D8} = {result is not null}");
            Assert.That(result, Is.Not.Null, $"Key range:{idx:D8} should exist");
        }
    }
}
