using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

public readonly record struct Vector3Int(int X, int Y, int Z)
{
    public static Vector3Int Zero => new(0, 0, 0);

    public static Vector3Int One => new(1, 1, 1);

    public static Vector3Int UnitX => new(1, 0, 0);

    public static Vector3Int UnitY => new(0, 1, 0);

    public static Vector3Int UnitZ => new(0, 0, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator +(Vector3Int left, Vector3Int right)
    {
        return new Vector3Int(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator -(Vector3Int left, Vector3Int right)
    {
        return new Vector3Int(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator *(Vector3Int vector, int scalar)
    {
        return new Vector3Int(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator *(int scalar, Vector3Int vector)
    {
        return new Vector3Int(scalar * vector.X, scalar * vector.Y, scalar * vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator /(Vector3Int vector, int scalar)
    {
        return new Vector3Int(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator -(Vector3Int vector)
    {
        return new Vector3Int(-vector.X, -vector.Y, -vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DistanceSquared(Vector3Int a, Vector3Int b)
    {
        return (a - b).LengthSquared();
    }
}
