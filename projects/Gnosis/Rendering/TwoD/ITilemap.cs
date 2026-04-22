namespace Gnosis.Rendering.TwoD;

public interface ITile
{
    int Id { get; }
    ISprite? Sprite { get; }
    int[] Position { get; }
    bool IsEmpty { get; }
}

public interface ITilemap
{
    string Name { get; }
    int Width { get; }
    int Height { get; }
    float TileSize { get; set; }
    ITile GetTile(int x, int y);
    void SetTile(int x, int y, ITile tile);
    void RemoveTile(int x, int y);
    void Clear();
    void Refresh();
}

public interface ITilemapRenderer
{
    string SortingLayer { get; set; }
    int SortingOrder { get; set; }
    float[] Color { get; set; }
    void Render();
}
