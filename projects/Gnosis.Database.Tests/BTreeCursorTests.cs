using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database.Tests;

[TestFixture]
public class BTreeCursorTests
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
    public void SeekToFirst_EmptyTree_ReturnsFalse()
    {
        using var cursor = _tree.CreateCursor();
        Assert.That(cursor.SeekToFirst(), Is.False);
    }

    [Test]
    public async Task SeekToFirst_SingleKey_ReturnsTrue()
    {
        await _tree.InsertAsync(DatabaseKey.FromString("a"), DatabaseValue.FromString("1"));

        using var cursor = _tree.CreateCursor();
        Assert.That(cursor.SeekToFirst(), Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromString("a")), Is.EqualTo(0));
    }

    [Test]
    public async Task Seek_ExistingKey_ReturnsTrue()
    {
        await _tree.InsertAsync(DatabaseKey.FromString("b"), DatabaseValue.FromString("2"));

        using var cursor = _tree.CreateCursor();
        Assert.That(cursor.Seek(DatabaseKey.FromString("b")), Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromString("b")), Is.EqualTo(0));
    }

    [Test]
    public async Task Seek_NonExistingKey_ReturnsNextGreater()
    {
        await _tree.InsertAsync(DatabaseKey.FromString("a"), DatabaseValue.FromString("1"));
        await _tree.InsertAsync(DatabaseKey.FromString("c"), DatabaseValue.FromString("3"));

        using var cursor = _tree.CreateCursor();
        Assert.That(cursor.Seek(DatabaseKey.FromString("b")), Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromString("c")), Is.EqualTo(0));
    }

    [Test]
    public async Task MoveNext_TraversesAllKeys()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromString($"key{i}"), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        Assert.That(cursor.SeekToFirst(), Is.True);

        var count = 0;
        while (cursor.IsValid)
        {
            count++;
            if (!cursor.MoveNext())
            {
                break;
            }
        }

        Assert.That(count, Is.EqualTo(5));
    }

    [Test]
    public async Task MoveNext_LeafLink_TraversesInOrder()
    {
        for (var i = 10; i >= 1; i--)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        var found = cursor.Seek(DatabaseKey.FromUInt64(1));
        TestContext.WriteLine($"Seek returned: {found}, IsValid: {cursor.IsValid}");

        if (cursor.IsValid)
        {
            TestContext.WriteLine($"Current key bytes: {BitConverter.ToString(cursor.Current.Key.Bytes.ToArray())}");
        }

        var keys = new List<ulong>();
        while (cursor.IsValid)
        {
            keys.Add(BitConverter.ToUInt64(cursor.Current.Key.Bytes.Span));
            if (!cursor.MoveNext())
            {
                break;
            }
        }

        TestContext.WriteLine($"Keys count: {keys.Count}");
        foreach (var k in keys)
        {
            TestContext.WriteLine($"Key: {k}");
        }

        Assert.That(keys, Is.Ordered);
        Assert.That(keys.Count, Is.EqualTo(10));
    }
}
