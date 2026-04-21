namespace Gnosis.Database.Core;

public readonly record struct DatabaseValue(ReadOnlyMemory<byte> Bytes)
{
    public int Length => Bytes.Length;

    public bool IsEmpty => Bytes.IsEmpty;

    public static DatabaseValue Empty => new(ReadOnlyMemory<byte>.Empty);

    public static DatabaseValue FromString(string value) =>
        new(System.Text.Encoding.UTF8.GetBytes(value));

    public static DatabaseValue FromInt32(int value) =>
        new(BitConverter.GetBytes(value));

    public static DatabaseValue FromInt64(long value) =>
        new(BitConverter.GetBytes(value));

    public static DatabaseValue FromDouble(double value) =>
        new(BitConverter.GetBytes(value));
}
