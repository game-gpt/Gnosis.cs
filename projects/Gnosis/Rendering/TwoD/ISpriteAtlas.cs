namespace Gnosis.Rendering.TwoD;

public interface ISpriteAtlas
{
    string Name { get; }
    string TexturePath { get; }
    IReadOnlyList<ISprite> Sprites { get; }
    ISprite GetSprite(string name);
    void Load();
    void Unload();
}
