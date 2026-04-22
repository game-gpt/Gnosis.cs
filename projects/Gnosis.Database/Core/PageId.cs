namespace Gnosis.Database.Core;

public readonly record struct PageId(long Value)
{
    public static readonly PageId Invalid = new(-1);

    public static readonly PageId First = new(0);
}
