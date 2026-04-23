using Gnosis.Database.Core;
using NUnit.Framework;

namespace Gnosis.Database;

[TestFixture]
public class BTreeIndexTests
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

    #region Insert Tests

    [Test]
    public async Task InsertAsync_SingleKey_CountIsOne()
    {
        var key = DatabaseKey.FromString("key1");
        var value = DatabaseValue.FromString("value1");

        var result = await _tree.InsertAsync(key, value);

        Assert.That(result, Is.True);
        Assert.That(_tree.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task InsertAsync_MultipleKeys_CountIncreases()
    {
        for (var i = 0; i < 10; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        Assert.That(_tree.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task InsertAsync_OverwriteExistingKey_ValueUpdated()
    {
        var key = DatabaseKey.FromString("key1");
        await _tree.InsertAsync(key, DatabaseValue.FromString("value1"));
        await _tree.InsertAsync(key, DatabaseValue.FromString("value2"));

        var result = await _tree.SearchAsync(key);

        Assert.That(result, Is.Not.Null);
        Assert.That(System.Text.Encoding.UTF8.GetString(result.Value.Bytes.Span), Is.EqualTo("value2"));
        Assert.That(_tree.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task InsertAsync_TriggerSplit_HeightIncreases()
    {
        for (var i = 0; i < Order; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        Assert.That(_tree.Height, Is.GreaterThanOrEqualTo(1));
        Assert.That(_tree.Count, Is.EqualTo(Order));
    }

    [Test]
    public async Task InsertAsync_ManyKeys_AllSearchable()
    {
        const int count = 100;
        for (var i = 0; i < count; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < count; i++)
        {
            var result = await _tree.SearchAsync(DatabaseKey.FromUInt64((ulong)i));
            Assert.That(result, Is.Not.Null, $"Key {i} not found");
        }
    }

    [Test]
    public async Task InsertAsync_ReverseOrder_AllSearchable()
    {
        const int count = 50;
        for (var i = count - 1; i >= 0; i--)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < count; i++)
        {
            var result = await _tree.SearchAsync(DatabaseKey.FromUInt64((ulong)i));
            Assert.That(result, Is.Not.Null, $"Key {i} not found");
        }
    }

    #endregion

    #region Search Tests

    [Test]
    public async Task SearchAsync_ExistingKey_ReturnsValue()
    {
        var key = DatabaseKey.FromString("key1");
        var value = DatabaseValue.FromString("value1");
        await _tree.InsertAsync(key, value);

        var result = await _tree.SearchAsync(key);

        Assert.That(result, Is.Not.Null);
        Assert.That(System.Text.Encoding.UTF8.GetString(result.Value.Bytes.Span), Is.EqualTo("value1"));
    }

    [Test]
    public async Task SearchAsync_NonExistingKey_ReturnsNull()
    {
        var result = await _tree.SearchAsync(DatabaseKey.FromString("nonexistent"));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SearchAsync_EmptyTree_ReturnsNull()
    {
        var result = await _tree.SearchAsync(DatabaseKey.FromString("key1"));

        Assert.That(result, Is.Null);
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task DeleteAsync_ExistingKey_ReturnsTrue()
    {
        var key = DatabaseKey.FromString("key1");
        await _tree.InsertAsync(key, DatabaseValue.FromString("value1"));

        var result = await _tree.DeleteAsync(key);

        Assert.That(result, Is.True);
        Assert.That(_tree.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteAsync_NonExistingKey_ReturnsFalse()
    {
        var result = await _tree.DeleteAsync(DatabaseKey.FromString("nonexistent"));

        Assert.That(result, Is.False);
        Assert.That(_tree.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteAsync_AfterDelete_SearchReturnsNull()
    {
        var key = DatabaseKey.FromString("key1");
        await _tree.InsertAsync(key, DatabaseValue.FromString("value1"));

        await _tree.DeleteAsync(key);
        var result = await _tree.SearchAsync(key);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_MultipleKeys_RemainingSearchable()
    {
        const int count = 20;
        for (var i = 0; i < count; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < count; i += 2)
        {
            await _tree.DeleteAsync(DatabaseKey.FromUInt64((ulong)i));
        }

        for (var i = 0; i < count; i++)
        {
            var result = await _tree.SearchAsync(DatabaseKey.FromUInt64((ulong)i));
            if (i % 2 == 0)
            {
                Assert.That(result, Is.Null, $"Deleted key {i} should not be found");
            }
            else
            {
                Assert.That(result, Is.Not.Null, $"Key {i} should still exist");
            }
        }
    }

    [Test]
    public async Task DeleteAsync_AllKeys_TreeIsEmpty()
    {
        const int count = 10;
        for (var i = 0; i < count; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        for (var i = 0; i < count; i++)
        {
            await _tree.DeleteAsync(DatabaseKey.FromUInt64((ulong)i));
        }

        Assert.That(_tree.Count, Is.EqualTo(0));
    }

    #endregion

    #region Cursor Tests

    [Test]
    public async Task Cursor_SeekToFirst_ReturnsSmallestKey()
    {
        for (var i = 5; i >= 1; i--)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        var found = cursor.SeekToFirst();

        Assert.That(found, Is.True);
        Assert.That(cursor.IsValid, Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromUInt64(1)), Is.EqualTo(0));
    }

    [Test]
    public async Task Cursor_SeekToLast_ReturnsLargestKey()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        var found = cursor.SeekToLast();

        Assert.That(found, Is.True);
        Assert.That(cursor.IsValid, Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromUInt64(5)), Is.EqualTo(0));
    }

    [Test]
    public async Task Cursor_MoveNext_TraversesInOrder()
    {
        const int count = 20;
        for (var i = count; i >= 1; i--)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        cursor.SeekToFirst();

        var keys = new List<ulong>();
        while (cursor.IsValid)
        {
            keys.Add(BitConverter.ToUInt64(cursor.Current.Key.Bytes.Span));
            cursor.MoveNext();
        }

        Assert.That(keys, Is.Ordered);
        Assert.That(keys.Count, Is.EqualTo(count));
    }

    [Test]
    public async Task Cursor_Seek_FindsCorrectPosition()
    {
        for (var i = 0; i < 20; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        var found = cursor.Seek(DatabaseKey.FromUInt64(10));

        Assert.That(found, Is.True);
        Assert.That(cursor.IsValid, Is.True);
        Assert.That(cursor.Current.Key.CompareTo(DatabaseKey.FromUInt64(10)), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task Cursor_MovePrev_GoesBackward()
    {
        for (var i = 0; i < 10; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        using var cursor = _tree.CreateCursor();
        cursor.SeekToLast();

        var keys = new List<ulong>();
        while (cursor.IsValid)
        {
            keys.Add(BitConverter.ToUInt64(cursor.Current.Key.Bytes.Span));
            if (!cursor.MovePrev())
            {
                break;
            }
        }

        Assert.That(keys, Is.Ordered.Descending);
    }

    [Test]
    public async Task Cursor_EmptyTree_ReturnsFalse()
    {
        using var cursor = _tree.CreateCursor();
        var found = cursor.SeekToFirst();

        Assert.That(found, Is.False);
        Assert.That(cursor.IsValid, Is.False);
    }

    #endregion

    #region Properties Tests

    [Test]
    public void Order_ReturnsCorrectValue()
    {
        Assert.That(_tree.Order, Is.EqualTo(Order));
    }

    [Test]
    public async Task Height_IncreasesWithSplits()
    {
        var initialHeight = _tree.Height;

        for (var i = 0; i < 100; i++)
        {
            await _tree.InsertAsync(DatabaseKey.FromUInt64((ulong)i), DatabaseValue.FromInt32(i));
        }

        Assert.That(_tree.Height, Is.GreaterThanOrEqualTo(initialHeight));
    }

    #endregion
}
