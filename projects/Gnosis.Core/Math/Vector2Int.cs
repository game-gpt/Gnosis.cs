using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

public readonly record struct Vector2Int(int X, int Y)
{
    public static Vector2Int Zero => new(0, 0);

    public static Vector2Int One => new(1, 1);

    public static Vector2Int UnitX => new(1, 0);

    public static Vector2Int UnitY => new(0, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator +(Vector2Int left, Vector2Int right)
    {
        return new Vector2Int(left.X + right.X, left.Y + right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int left, Vector2Int right)
    {
        return new Vector2Int(left.X - right.X, left.Y - right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(Vector2Int vector, int scalar)
    {
        return new Vector2Int(vector.X * scalar, vector.Y * scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(int scalar, Vector2Int vector)
    {
        return new Vector2Int(scalar * vector.X, scalar * vector.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator /(Vector2Int vector, int scalar)
    {
        return new Vector2Int(vector.X / scalar, vector.Y / scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int vector)
    {
        return new Vector2Int(-vector.X, -vector.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance()
    {
        return MathF.Sqrt(X * X + Y * Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LengthSquared()
    {
        return X * X + Y * Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DistanceSquared(Vector2Int a, Vector2Int b)
    {
        return (a - b).LengthSquared();
    }
}
