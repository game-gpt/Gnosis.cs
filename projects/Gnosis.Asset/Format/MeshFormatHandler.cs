using System.Text.Json;
using Gnosis.Asset.Format.MeshOptimization;
using Gnosis.Asset.Format.MeshParsers;
using Oak.Obj;

namespace Gnosis.Asset.Format;

public class MeshFormatHandler : FormatHandlerBase, IMeshFormat
{
    #region 属性

    public override FormatType SupportedFormat => FormatType.Mesh;

    #endregion

    #region 公开方法

    /// <summary>
    /// 从指定路径加载网格数据，支持引擎格式、OBJ、glTF 和 GLB
    /// </summary>
    public async Task<MeshData> LoadMeshAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到网格文件：{path}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".gnosis-mesh" or ".mesh" or ".scirptmesh" => await LoadEngineFormatAsync(path, cancellationToken),
            ".obj" => await LoadObjAsync(path, cancellationToken),
            ".gltf" => await LoadGltfAsync(path, cancellationToken),
            ".glb" => await LoadGlbAsync(path, cancellationToken),
            ".fbx" => throw new NotSupportedException("FBX 格式需要 Autodesk FBX SDK 支持，建议转换为 glTF"),
            _ => throw new NotSupportedException($"不支持的网格格式：{extension}")
        };
    }

    /// <summary>
    /// 将网格数据保存到指定路径
    /// </summary>
    public async Task SaveMeshAsync(string path, MeshData mesh, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        switch (extension)
        {
            case ".gnosis-mesh":
            case ".mesh":
            case ".scirptmesh":
                await SaveEngineFormatAsync(path, mesh, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"不支持的导出格式：{extension}");
        }
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
    public override async Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".obj")
        {
            return await ValidateObjAsync(path, cancellationToken);
        }

        if (extension == ".gltf")
        {
            return await ValidateGltfAsync(path, cancellationToken);
        }

        if (extension == ".glb")
        {
            return await ValidateGlbAsync(path, cancellationToken);
        }

        try
        {
            var mesh = await LoadMeshAsync(path, cancellationToken);
            return ValidateMeshData(mesh);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    #endregion

    #region 保护方法

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-mesh", ".mesh", ".obj", ".fbx", ".gltf", ".glb", ".scirptmesh" };
    }

    #endregion

    #region 引擎格式加载/保存

    private async Task<MeshData> LoadEngineFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<MeshData>(data)
            ?? throw new InvalidOperationException($"反序列化网格失败：{path}");
    }

    private async Task SaveEngineFormatAsync(string path, MeshData mesh, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(mesh, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await WriteAsync(path, data, null, cancellationToken);
    }

    #endregion

    #region OBJ 格式加载

    private async Task<MeshData> LoadObjAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var content = System.Text.Encoding.UTF8.GetString(data);
        var fileName = Path.GetFileNameWithoutExtension(path);

        var parser = new Oak.Obj.ObjParser();
        var result = parser.Parse(content.AsSpan(), fileName);

        return new MeshData
        {
            Name = fileName,
            Vertices = result.Vertices.Select(v => new MeshVertex
            {
                Position = v.Position,
                Normal = v.Normal ?? [],
                Uv = v.Uv ?? []
            }).ToList(),
            Indices = result.Indices,
            SubMeshes = result.SubMeshes.Select(s => new MeshSubMesh
            {
                IndexStart = s.IndexStart,
                IndexCount = s.IndexCount,
                MaterialPath = s.MaterialPath
            }).ToList(),
            Bounds = new MeshBounds
            {
                Center = result.Bounds.Center,
                Extents = result.Bounds.Extents
            }
        };
    }

    private async Task<bool> ValidateObjAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var data = await ReadAsync(path, cancellationToken);
            var content = System.Text.Encoding.UTF8.GetString(data);

            var hasVertex = false;
            var hasFace = false;

            using var reader = new StringReader(content);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.TrimStart();

                if (trimmed.StartsWith("v "))
                {
                    hasVertex = true;
                }
                else if (trimmed.StartsWith("f "))
                {
                    hasFace = true;
                }

                if (hasVertex && hasFace)
                {
                    return true;
                }
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    #endregion

    #region glTF/GLB 格式加载

    private async Task<MeshData> LoadGltfAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var basePath = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);

        var adapter = new GltfMeshAdapter();
        var result = adapter.LoadGltf(System.Text.Encoding.UTF8.GetString(data), basePath);

        return result with { Name = fileName };
    }

    private async Task<MeshData> LoadGlbAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var fileName = Path.GetFileNameWithoutExtension(path);

        var adapter = new GltfMeshAdapter();
        var result = adapter.LoadGlb(data);

        return result with { Name = fileName };
    }

    private async Task<bool> ValidateGltfAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var data = await ReadAsync(path, cancellationToken);
            var json = System.Text.Encoding.UTF8.GetString(data);

            if (!json.Contains("\"meshes\"") && !json.Contains("\"accessors\""))
            {
                return false;
            }

            var mesh = await LoadGltfAsync(path, cancellationToken);
            return ValidateMeshData(mesh);
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<bool> ValidateGlbAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var data = await ReadAsync(path, cancellationToken);

            if (data.Length < 4)
            {
                return false;
            }

            var magic = BitConverter.ToUInt32(data, 0);
            if (magic != 0x46546C67)
            {
                return false;
            }

            var mesh = await LoadGlbAsync(path, cancellationToken);
            return ValidateMeshData(mesh);
        }
        catch (InvalidDataException)
        {
            return false;
        }
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
