namespace Gnosis.Core;

public readonly record struct Position(float X, float Y, float Z)
{
    public float DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public float DistanceToSquared(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
