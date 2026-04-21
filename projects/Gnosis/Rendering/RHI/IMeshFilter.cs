namespace Gnosis.Rendering.RHI;

public interface IMeshFilter
{
    ulong MeshHandle { get; }
    uint SubMeshIndex { get; }
}
