namespace Gnosis.Rendering.TwoD;

public interface ISpriteRenderer
{
    ISprite? Sprite { get; set; }
    float[] Color { get; set; }
    bool FlipX { get; set; }
    bool FlipY { get; set; }
    int SortingOrder { get; set; }
    string SortingLayer { get; set; }
    float[] Size { get; set; }
    void Draw();
}
