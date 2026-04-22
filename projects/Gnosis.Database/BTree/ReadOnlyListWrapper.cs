using System.Collections;

namespace Gnosis.Database.BTree;

internal sealed class ReadOnlyListWrapper<T> : IReadOnlyList<T>
{
    private readonly T[] _array;
    private readonly int _count;

    public ReadOnlyListWrapper(T[] array, int count)
    {
        _array = array;
        _count = count;
    }

    public T this[int index] => index >= 0 && index < _count
        ? _array[index]
        : throw new ArgumentOutOfRangeException(nameof(index));

    public int Count => _count;

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return _array[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
