namespace Gnosis.Navigation.Tile;

/// <summary>
/// 导航网格分块坐标
/// </summary>
public readonly record struct TileCoord(int X, int Y, int Z)
{
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
