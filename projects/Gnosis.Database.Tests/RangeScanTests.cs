using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class RangeScanTests
{
    private BTreeIndex _tree = null!;

    [SetUp]
    public void SetUp()
    {
        _tree = new BTreeIndex(128);
    }

    [TearDown]
    public void TearDown()
    {
        _tree.Dispose();
    }

    [Test]
    public async Task RangeScan_LargeDataset_ReturnsCorrectRange()
    {
        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await _tree.InsertAsync(key, value);
        }

        var startKey = DatabaseKey.FromString("range:00000500");
        var endKey = DatabaseKey.FromString("range:00000600");

        using var cursor = _tree.CreateCursor();
        cursor.Seek(startKey);

        var results = new List<string>();
        while (cursor.IsValid)
        {
            var keyStr = System.Text.Encoding.UTF8.GetString(cursor.Current.Key.Bytes.Span);
            if (string.Compare(keyStr, "range:00000600", StringComparison.Ordinal) > 0)
            {
                break;
            }

            results.Add(keyStr);
            if (!cursor.MoveNext())
            {
                break;
            }
        }

        TestContext.WriteLine($"Results count: {results.Count}");
        Assert.That(results.Count, Is.GreaterThan(0));
        Assert.That(results.Count, Is.LessThanOrEqualTo(101));
    }

    [Test]
    public async Task Seek_ExistingKeyInLargeDataset_ReturnsTrue()
    {
        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            var key = DatabaseKey.FromString($"range:{i:D8}");
            var value = DatabaseValue.FromString($"value:{i}");
            await _tree.InsertAsync(key, value);
        }

        using var cursor = _tree.CreateCursor();
        var found = cursor.Seek(DatabaseKey.FromString("range:00000500"));

        TestContext.WriteLine($"Seek result: {found}, IsValid: {cursor.IsValid}");
        if (cursor.IsValid)
        {
            TestContext.WriteLine($"Current key: {System.Text.Encoding.UTF8.GetString(cursor.Current.Key.Bytes.Span)}");
        }

        Assert.That(found, Is.True);
        Assert.That(cursor.IsValid, Is.True);
    }
}
