using System.Text.Json;
using Gnosis.Assets.Formats.MeshOptimization;

namespace Gnosis.Assets.Formats;

public class MeshFormatHandler : FormatHandlerBase, IMeshFormat
{
    #region 属性

    public override FormatType SupportedFormat => FormatType.Mesh;

    #endregion

    #region 公开方法

    /// <summary>
    /// 从指定路径加载网格数据
    /// </summary>
    public async Task<MeshData> LoadMeshAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<MeshData>(data)
            ?? throw new InvalidOperationException($"反序列化网格失败：{path}");
    }

    /// <summary>
    /// 将网格数据保存到指定路径
    /// </summary>
    public async Task SaveMeshAsync(string path, MeshData mesh, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(mesh, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await WriteAsync(path, data, null, cancellationToken);
    }

    /// <summary>
    /// 对网格数据执行优化
    /// </summary>
    public Task<MeshData> OptimizeAsync(MeshData mesh, MeshOptimizationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MeshOptimizationOptions();

        var vertices = mesh.Vertices.ToList();
        var indices = mesh.Indices.ToList();

        if (options.GenerateNormals)
        {
            vertices = GenerateNormalsForVertices(vertices, indices);
        }

        if (options.GenerateTangents)
        {
            vertices = GenerateTangentsForVertices(vertices, indices);
        }

        if (options.Simplify)
        {
            var (newVertices, newIndices) = EdgeCollapser.Simplify(vertices, indices, options.SimplifyTarget);
            vertices = newVertices;
            indices = newIndices;
        }

        if (options.OptimizeIndices)
        {
            indices = OptimizeIndexBuffer(vertices, indices, out var vertexRemap);

            if (options.OptimizeVertices)
            {
                vertices = ReorderVertices(vertices, vertexRemap);
                indices = RemapIndices(indices, vertexRemap);
            }
        }

        var result = mesh with
        {
            Vertices = vertices,
            Indices = indices
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// 验证网格数据完整性
    /// </summary>
    public override Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var mesh = LoadMeshAsync(path, cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(ValidateMeshData(mesh));
        }
        catch (JsonException)
        {
            return Task.FromResult(false);
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region 保护方法

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-mesh", ".mesh", ".obj", ".fbx", ".gltf", ".glb", ".scirptmesh" };
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 验证网格数据是否有效
    /// </summary>
    private static bool ValidateMeshData(MeshData mesh)
    {
        if (mesh.Indices.Count < 3)
        {
            return false;
        }

        if (mesh.Vertices.Count == 0)
        {
            return false;
        }

        foreach (int index in mesh.Indices)
        {
            if (index < 0 || index >= mesh.Vertices.Count)
            {
                return false;
            }
        }

        bool hasNonZeroPosition = false;
        foreach (var vertex in mesh.Vertices)
        {
            if (vertex.Position is { Length: >= 3 })
            {
                float lenSq = vertex.Position[0] * vertex.Position[0]
                              + vertex.Position[1] * vertex.Position[1]
                              + vertex.Position[2] * vertex.Position[2];

                if (lenSq > 1e-10f)
                {
                    hasNonZeroPosition = true;
                    break;
                }
            }
        }

        if (!hasNonZeroPosition)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 为缺少法线的顶点生成法线
    /// </summary>
    private static List<MeshVertex> GenerateNormalsForVertices(List<MeshVertex> vertices, List<int> indices)
    {
        var normals = NormalGenerator.Generate(vertices, indices);
        var result = new List<MeshVertex>(vertices.Count);

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];

            if (v.Normal is not { Length: >= 3 })
            {
                result.Add(v with { Normal = normals[i] });
            }
            else
            {
                result.Add(v);
            }
        }

        return result;
    }

    /// <summary>
    /// 为拥有法线和 UV 的顶点生成切线
    /// </summary>
    private static List<MeshVertex> GenerateTangentsForVertices(List<MeshVertex> vertices, List<int> indices)
    {
        var tangents = TangentGenerator.Generate(vertices, indices);
        var result = new List<MeshVertex>(vertices.Count);

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];

            bool hasNormal = v.Normal is { Length: >= 3 };
            bool hasUv = v.Uv is { Length: >= 2 };

            if (hasNormal && hasUv && v.Tangent is not { Length: >= 4 })
            {
                result.Add(v with { Tangent = tangents[i] });
            }
            else
            {
                result.Add(v);
            }
        }

        return result;
    }

    /// <summary>
    /// 使用 Forsyth 算法优化索引缓冲区
    /// </summary>
    private static List<int> OptimizeIndexBuffer(List<MeshVertex> vertices, List<int> indices,
        out int[] vertexRemap)
    {
        var indexArray = indices.ToArray();
        var optimized = ForsythVertexCacheOptimizer.Optimize(indexArray);

        vertexRemap = ComputeVertexRemap(vertices.Count, optimized);

        return optimized.ToList();
    }

    /// <summary>
    /// 计算顶点重排映射，使按索引首次引用顺序排列
    /// </summary>
    private static int[] ComputeVertexRemap(int vertexCount, int[] optimizedIndices)
    {
        var remap = new int[vertexCount];
        Array.Fill(remap, -1);

        int nextIndex = 0;

        foreach (int idx in optimizedIndices)
        {
            if (remap[idx] < 0)
            {
                remap[idx] = nextIndex++;
            }
        }

        for (int i = 0; i < vertexCount; i++)
        {
            if (remap[i] < 0)
            {
                remap[i] = nextIndex++;
            }
        }

        return remap;
    }

    /// <summary>
    /// 根据重排映射重排顶点缓冲区
    /// </summary>
    private static List<MeshVertex> ReorderVertices(List<MeshVertex> vertices, int[] vertexRemap)
    {
        var newVertices = new MeshVertex[vertexRemap.Length];

        for (int i = 0; i < vertexRemap.Length; i++)
        {
            newVertices[vertexRemap[i]] = vertices[i];
        }

        return newVertices.ToList();
    }

    /// <summary>
    /// 根据重排映射更新索引缓冲区
    /// </summary>
    private static List<int> RemapIndices(List<int> indices, int[] vertexRemap)
    {
        var newIndices = new int[indices.Count];

        for (int i = 0; i < indices.Count; i++)
        {
            newIndices[i] = vertexRemap[indices[i]];
        }

        return newIndices.ToList();
    }

    #endregion
}
