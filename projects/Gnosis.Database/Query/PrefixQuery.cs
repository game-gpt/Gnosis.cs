using Gnosis.Database.Core;

namespace Gnosis.Database.Query;

public sealed class PrefixQuery
{
    #region 公开方法

    public static IReadOnlyList<DatabaseEntry> Execute(ICursor cursor, DatabaseKey prefix, int limit = 1000)
    {
        var results = new List<DatabaseEntry>();

        if (!cursor.Seek(prefix))
        {
            if (!cursor.SeekToFirst())
            {
                return results;
            }

            while (cursor.IsValid)
            {
                if (cursor.Current.Key.StartsWith(prefix))
                {
                    break;
                }

                if (cursor.Current.Key.CompareTo(prefix) > 0)
                {
                    return results;
                }

                if (!cursor.MoveNext())
                {
                    return results;
                }
            }
        }

        while (cursor.IsValid && results.Count < limit)
        {
            if (!cursor.Current.Key.StartsWith(prefix))
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
