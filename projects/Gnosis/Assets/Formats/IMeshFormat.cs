namespace Gnosis.Assets.Formats;

public interface IMeshFormat : IFormatHandler
{
    Task<MeshData> LoadMeshAsync(string path, CancellationToken cancellationToken = default);
    Task SaveMeshAsync(string path, MeshData mesh, CancellationToken cancellationToken = default);
    Task<MeshData> OptimizeAsync(MeshData mesh, MeshOptimizationOptions? options = null, CancellationToken cancellationToken = default);
}

public record MeshData
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<MeshVertex> Vertices { get; init; } = new List<MeshVertex>();
    public IReadOnlyList<int> Indices { get; init; } = new List<int>();
    public IReadOnlyList<MeshSubMesh> SubMeshes { get; init; } = new List<MeshSubMesh>();
    public IReadOnlyList<MeshBone> Bones { get; init; } = new List<MeshBone>();
    public MeshBounds Bounds { get; init; } = new();
}

public record MeshVertex
{
    public float[] Position { get; init; } = Array.Empty<float>();
    public float[] Normal { get; init; } = Array.Empty<float>();
    public float[] Tangent { get; init; } = Array.Empty<float>();
    public float[] Uv { get; init; } = Array.Empty<float>();
    public float[] Uv2 { get; init; } = Array.Empty<float>();
    public float[] Color { get; init; } = Array.Empty<float>();
    public byte[] BoneIndices { get; init; } = Array.Empty<byte>();
    public float[] BoneWeights { get; init; } = Array.Empty<float>();
}

public record MeshSubMesh
{
    public int IndexStart { get; init; }
    public int IndexCount { get; init; }
    public string MaterialPath { get; init; } = string.Empty;
}

public record MeshBone
{
    public string Name { get; init; } = string.Empty;
    public int ParentIndex { get; init; } = -1;
    public float[] BindPose { get; init; } = Array.Empty<float>();
    public float[] InverseBindPose { get; init; } = Array.Empty<float>();
}

public record MeshBounds
{
    public float[] Center { get; init; } = Array.Empty<float>();
    public float[] Extents { get; init; } = Array.Empty<float>();
}

public record MeshOptimizationOptions
{
    public bool OptimizeIndices { get; init; } = true;
    public bool OptimizeVertices { get; init; } = true;
    public bool GenerateNormals { get; init; }
    public bool GenerateTangents { get; init; }
    public bool Simplify { get; init; }
    public float SimplifyTarget { get; init; } = 0.5f;
}
