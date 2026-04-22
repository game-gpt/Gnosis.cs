using System.Text.Json;

namespace Gnosis.Asset.Format.MeshParsers;

public sealed class GltfParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GltfParseResult ParseGltf(ReadOnlySpan<byte> jsonBytes, string? basePath = null)
    {
        var gltf = JsonSerializer.Deserialize<GltfDocument>(jsonBytes, JsonOptions)
            ?? throw new InvalidDataException("glTF JSON 反序列化失败");

        return BuildMeshData(gltf, null, basePath);
    }

    public GltfParseResult ParseGlb(ReadOnlySpan<byte> glbBytes, string? basePath = null)
    {
        if (glbBytes.Length < 12)
        {
            throw new InvalidDataException("GLB 文件过小");
        }

        var magic = BitConverter.ToUInt32(glbBytes[..4]);
        if (magic != 0x46546C67)
        {
            throw new InvalidDataException("GLB 魔数无效");
        }

        var version = BitConverter.ToUInt32(glbBytes[4..8]);
        if (version != 2)
        {
            throw new InvalidDataException($"不支持的 GLB 版本：{version}");
        }

        var totalLength = (int)BitConverter.ToUInt32(glbBytes[8..12]);
        if (glbBytes.Length < totalLength)
        {
            throw new InvalidDataException("GLB 文件截断");
        }

        var offset = 12;
        byte[]? jsonChunk = null;
        byte[]? binChunk = null;

        while (offset + 8 <= totalLength)
        {
            var chunkLength = (int)BitConverter.ToUInt32(glbBytes[offset..(offset + 4)]);
            var chunkType = BitConverter.ToUInt32(glbBytes[(offset + 4)..(offset + 8)]);
            offset += 8;

            if (chunkType == 0x4E4F534A)
            {
                jsonChunk = glbBytes[offset..(offset + chunkLength)].ToArray();
            }
            else if (chunkType == 0x004E4942)
            {
                binChunk = glbBytes[offset..(offset + chunkLength)].ToArray();
            }

            offset += chunkLength;
        }

        if (jsonChunk == null)
        {
            throw new InvalidDataException("GLB 缺少 JSON 块");
        }

        var gltf = JsonSerializer.Deserialize<GltfDocument>(jsonChunk, JsonOptions)
            ?? throw new InvalidDataException("glTF JSON 反序列化失败");

        return BuildMeshData(gltf, binChunk, basePath);
    }

    private static GltfParseResult BuildMeshData(GltfDocument gltf, byte[]? glbBinChunk, string? basePath)
    {
        var result = new GltfParseResult();
        var allVertices = new List<MeshVertex>();
        var allIndices = new List<int>();
        var subMeshes = new List<MeshSubMesh>();

        foreach (var mesh in gltf.Meshes ?? [])
        {
            foreach (var primitive in mesh.Primitives ?? [])
            {
                var indexStart = allIndices.Count;
                var vertexOffset = allVertices.Count;

                var positionData = GetAccessorData(gltf, glbBinChunk, basePath, primitive.Attributes?.Position);
                var normalData = GetAccessorData(gltf, glbBinChunk, basePath, primitive.Attributes?.Normal);
                var uvData = GetAccessorData(gltf, glbBinChunk, basePath, primitive.Attributes?.TexCoord0);

                var vertexCount = positionData.Length / 3;

                for (var i = 0; i < vertexCount; i++)
                {
                    var vertex = new MeshVertex
                    {
                        Position = new float[3]
                    };

                    if (i * 3 + 2 < positionData.Length)
                    {
                        vertex.Position[0] = positionData[i * 3];
                        vertex.Position[1] = positionData[i * 3 + 1];
                        vertex.Position[2] = positionData[i * 3 + 2];
                    }

                    if (normalData.Length > 0 && i * 3 + 2 < normalData.Length)
                    {
                        vertex.Normal = new float[3];
                        vertex.Normal[0] = normalData[i * 3];
                        vertex.Normal[1] = normalData[i * 3 + 1];
                        vertex.Normal[2] = normalData[i * 3 + 2];
                    }

                    if (uvData.Length > 0 && i * 2 + 1 < uvData.Length)
                    {
                        vertex.Uv = new float[2];
                        vertex.Uv[0] = uvData[i * 2];
                        vertex.Uv[1] = uvData[i * 2 + 1];
                    }

                    allVertices.Add(vertex);
                }

                if (primitive.Indices.HasValue)
                {
                    var indexData = GetAccessorData(gltf, glbBinChunk, basePath, primitive.Indices.Value);
                    var indexAccessor = gltf.Accessors![primitive.Indices.Value];
                    var componentType = indexAccessor.ComponentType;

                    for (var i = 0; i < indexAccessor.Count; i++)
                    {
                        int index;

                        if (componentType == 5121)
                        {
                            index = (int)indexData[i];
                        }
                        else if (componentType == 5123)
                        {
                            index = BitConverter.ToUInt16(indexData, i * 2);
                        }
                        else if (componentType == 5125)
                        {
                            index = (int)BitConverter.ToUInt32(indexData, i * 4);
                        }
                        else
                        {
                            throw new NotSupportedException($"不支持的索引组件类型：{componentType}");
                        }

                        allIndices.Add(index + vertexOffset);
                    }
                }
                else
                {
                    for (var i = 0; i < vertexCount; i++)
                    {
                        allIndices.Add(i + vertexOffset);
                    }
                }

                subMeshes.Add(new MeshSubMesh
                {
                    IndexStart = indexStart,
                    IndexCount = allIndices.Count - indexStart,
                    MaterialPath = primitive.Material.HasValue
                        ? $"material_{primitive.Material.Value}"
                        : string.Empty
                });
            }
        }

        var bounds = ComputeBounds(allVertices);

        result.Vertices = allVertices;
        result.Indices = allIndices;
        result.SubMeshes = subMeshes;
        result.Bounds = bounds;

        return result;
    }

    private static byte[] GetAccessorData(GltfDocument gltf, byte[]? glbBinChunk, string? basePath, int? accessorIndex)
    {
        if (!accessorIndex.HasValue || gltf.Accessors == null || gltf.BufferViews == null)
        {
            return [];
        }

        var accessor = gltf.Accessors[accessorIndex.Value];
        var bufferView = gltf.BufferViews[accessor.BufferView];

        var bufferData = GetBufferData(gltf, glbBinChunk, basePath, bufferView.Buffer);
        var offset = (accessor.ByteOffset ?? 0) + (bufferView.ByteOffset ?? 0);
        var length = accessor.Count * GetComponentSize(accessor.ComponentType) * GetNumComponents(accessor.Type);

        if (offset + length > bufferData.Length)
        {
            return [];
        }

        var result = new byte[length];
        Array.Copy(bufferData, offset, result, 0, length);

        return result;
    }

    private static byte[] GetBufferData(GltfDocument gltf, byte[]? glbBinChunk, string? basePath, int bufferIndex)
    {
        var buffer = gltf.Buffers![bufferIndex];

        if (bufferIndex == 0 && glbBinChunk != null)
        {
            return glbBinChunk;
        }

        if (buffer.Uri == null)
        {
            return glbBinChunk ?? [];
        }

        if (buffer.Uri.StartsWith("data:"))
        {
            var base64Start = buffer.Uri.IndexOf("base64,", StringComparison.Ordinal);
            if (base64Start >= 0)
            {
                var base64Data = buffer.Uri[(base64Start + 7)..];
                return Convert.FromBase64String(base64Data);
            }
        }

        if (basePath != null && !buffer.Uri.StartsWith("data:"))
        {
            var externalPath = Path.Combine(basePath, buffer.Uri);
            if (File.Exists(externalPath))
            {
                return File.ReadAllBytes(externalPath);
            }
        }

        return [];
    }

    private static int GetComponentSize(int componentType)
    {
        return componentType switch
        {
            5120 => 1,
            5121 => 1,
            5122 => 2,
            5123 => 2,
            5125 => 4,
            5126 => 4,
            _ => throw new NotSupportedException($"不支持的组件类型：{componentType}")
        };
    }

    private static int GetNumComponents(string type)
    {
        return type switch
        {
            "SCALAR" => 1,
            "VEC2" => 2,
            "VEC3" => 3,
            "VEC4" => 4,
            "MAT2" => 4,
            "MAT3" => 9,
            "MAT4" => 16,
            _ => throw new NotSupportedException($"不支持的类型：{type}")
        };
    }

    private static MeshBounds ComputeBounds(List<MeshVertex> vertices)
    {
        if (vertices.Count == 0)
        {
            return new MeshBounds();
        }

        var min = new float[] { float.MaxValue, float.MaxValue, float.MaxValue };
        var max = new float[] { float.MinValue, float.MinValue, float.MinValue };

        foreach (var vertex in vertices)
        {
            if (vertex.Position is { Length: >= 3 })
            {
                for (var i = 0; i < 3; i++)
                {
                    if (vertex.Position[i] < min[i]) min[i] = vertex.Position[i];
                    if (vertex.Position[i] > max[i]) max[i] = vertex.Position[i];
                }
            }
        }

        var center = new float[3];
        var extents = new float[3];

        for (var i = 0; i < 3; i++)
        {
            center[i] = (min[i] + max[i]) * 0.5f;
            extents[i] = (max[i] - min[i]) * 0.5f;
        }

        return new MeshBounds { Center = center, Extents = extents };
    }
}

public class GltfParseResult
{
    public List<MeshVertex> Vertices { get; init; } = [];
    public List<int> Indices { get; init; } = [];
    public List<MeshSubMesh> SubMeshes { get; init; } = [];
    public MeshBounds Bounds { get; init; } = new();
}

internal class GltfDocument
{
    public List<GltfAccessor>? Accessors { get; init; }
    public List<GltfBuffer>? Buffers { get; init; }
    public List<GltfBufferView>? BufferViews { get; init; }
    public List<GltfMesh>? Meshes { get; init; }
}

internal class GltfAccessor
{
    public int BufferView { get; init; }
    public int ByteOffset { get; init; }
    public int ComponentType { get; init; }
    public int Count { get; init; }
    public string Type { get; init; } = "SCALAR";
}

internal class GltfBuffer
{
    public string? Uri { get; init; }
    public int ByteLength { get; init; }
}

internal class GltfBufferView
{
    public int Buffer { get; init; }
    public int ByteOffset { get; init; }
    public int ByteLength { get; init; }
    public int? Target { get; init; }
}

internal class GltfMesh
{
    public string? Name { get; init; }
    public List<GltfMeshPrimitive>? Primitives { get; init; }
}

internal class GltfMeshPrimitive
{
    public GltfMeshPrimitiveAttributes? Attributes { get; init; }
    public int? Indices { get; init; }
    public int? Material { get; init; }
    public int Mode { get; init; } = 4;
}

internal class GltfMeshPrimitiveAttributes
{
    public int? Position { get; init; }
    public int? Normal { get; init; }
    public int? TexCoord0 { get; init; }
    public int? TexCoord1 { get; init; }
    public int? Color0 { get; init; }
    public int? Joints0 { get; init; }
    public int? Weights0 { get; init; }
}
