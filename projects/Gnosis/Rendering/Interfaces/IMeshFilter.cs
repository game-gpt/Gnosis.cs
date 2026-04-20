namespace Gnosis.Rendering.Interfaces;

public interface IMeshFilter
{
    ulong MeshHandle { get; }
    uint SubMeshIndex { get; }
}
