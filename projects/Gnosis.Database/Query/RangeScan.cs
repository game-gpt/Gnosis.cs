using Gnosis.Database.Core;

namespace Gnosis.Database.Query;

public sealed class RangeScan
{
    #region 公开方法

    public static IReadOnlyList<DatabaseEntry> Execute(ICursor cursor, DatabaseKey start, DatabaseKey end, int limit = 1000)
    {
        return cursor.GetRange(start, end, limit);
    }

    public static async ValueTask<IReadOnlyList<DatabaseEntry>> ExecuteAsync(
        ICursor cursor,
        DatabaseKey start,
        DatabaseKey end,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DatabaseEntry>();

        if (!cursor.Seek(start))
        {
            return results;
        }

        while (cursor.IsValid && results.Count < limit)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var cmp = cursor.Current.Key.CompareTo(end);
            if (cmp > 0)
            {
                break;
            }

            results.Add(cursor.Current);

            if (!cursor.MoveNext())
            {
                break;
            }
        }

        return results;
    }

    #endregion
}
