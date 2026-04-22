namespace Gnosis.Graphic.Light;

public interface ILightmap
{
    string Name { get; }
    int Width { get; }
    int Height { get; }
    bool IsBaked { get; }
    void Bake();
    void Clear();
}
