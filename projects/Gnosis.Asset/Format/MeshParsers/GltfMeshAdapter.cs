using System.Buffers.Binary;
using Acorn.Gltf.Data;
using Acorn.Gltf.Decode;

namespace Gnosis.Asset.Format.MeshParsers;

/// <summary>
///     GLTF/GLB 网格数据适配器，使用 Acorn.Gltf 解码并转换为 Gnosis MeshData
/// </summary>
public sealed class GltfMeshAdapter
{
    /// <summary>
    ///     从 GLTF JSON 文件加载网格数据
    /// </summary>
    public MeshData LoadGltf(string jsonContent, string? basePath = null)
    {
        var decoder = new GltfDecoder();
        var model = decoder.DecodeJson(jsonContent);
        return ExtractMeshData(model, basePath);
    }

    /// <summary>
    ///     从 GLB 二进制数据加载网格数据
    /// </summary>
    public MeshData LoadGlb(byte[] data, string? basePath = null)
    {
        var decoder = new GltfDecoder();
        var model = decoder.DecodeGlb(data);
        return ExtractMeshData(model, basePath);
    }

    private static MeshData ExtractMeshData(GltfModelData model, string? basePath)
    {
        var allVertices = new List<MeshVertex>();
        var allIndices = new List<int>();
        var subMeshes = new List<MeshSubMesh>();

        var bufferDataList = new List<byte[]>();

        foreach (var buffer in model.Buffers)
        {
            bufferDataList.Add(LoadBufferData(buffer, basePath));
        }

        foreach (var mesh in model.Meshes)
        {
            foreach (var primitive in mesh.Primitives)
            {
                var indexStart = allIndices.Count;
                var vertexOffset = allVertices.Count;

                var positionData = GetAccessorData(model, bufferDataList, primitive.Attributes.GetValueOrDefault("POSITION"));
                var normalData = GetAccessorData(model, bufferDataList, primitive.Attributes.GetValueOrDefault("NORMAL"));
                var uvData = GetAccessorData(model, bufferDataList, primitive.Attributes.GetValueOrDefault("TEXCOORD_0"));

                var vertexCount = positionData.Length / 12;

                for (var i = 0; i < vertexCount; i++)
                {
                    var position = new float[3];

                    if (i * 12 + 8 < positionData.Length)
                    {
                        position[0] = BitConverter.ToSingle(positionData, i * 12);
                        position[1] = BitConverter.ToSingle(positionData, i * 12 + 4);
                        position[2] = BitConverter.ToSingle(positionData, i * 12 + 8);
                    }

                    float[]? normal = null;

                    if (normalData.Length > 0 && i * 12 + 8 < normalData.Length)
                    {
                        normal = new float[3];
                        normal[0] = BitConverter.ToSingle(normalData, i * 12);
                        normal[1] = BitConverter.ToSingle(normalData, i * 12 + 4);
                        normal[2] = BitConverter.ToSingle(normalData, i * 12 + 8);
                    }

                    float[]? uv = null;

                    if (uvData.Length > 0 && i * 8 + 4 < uvData.Length)
                    {
                        uv = new float[2];
                        uv[0] = BitConverter.ToSingle(uvData, i * 8);
                        uv[1] = BitConverter.ToSingle(uvData, i * 8 + 4);
                    }

                    allVertices.Add(new MeshVertex
                    {
                        Position = position,
                        Normal = normal ?? [],
                        Uv = uv ?? []
                    });
                }

                if (primitive.Indices.HasValue)
                {
                    var indexData = GetAccessorData(model, bufferDataList, primitive.Indices.Value);
                    var indexAccessor = model.Accessors[primitive.Indices.Value];
                    var componentType = indexAccessor.ComponentType;

                    for (var i = 0; i < indexAccessor.Count; i++)
                    {
                        int index;

                        if (componentType == 5121)
                        {
                            index = indexData[i];
                        }
                        else if (componentType == 5123)
                        {
                            index = BinaryPrimitives.ReadUInt16LittleEndian(indexData.AsSpan(i * 2));
                        }
                        else if (componentType == 5125)
                        {
                            index = (int)BinaryPrimitives.ReadUInt32LittleEndian(indexData.AsSpan(i * 4));
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

        return new MeshData
        {
            Vertices = allVertices,
            Indices = allIndices,
            SubMeshes = subMeshes,
            Bounds = bounds
        };
    }

    private static byte[] GetAccessorData(GltfModelData model, List<byte[]> bufferDataList, int? accessorIndex)
    {
        if (!accessorIndex.HasValue || model.Accessors == null || model.BufferViews == null)
        {
            return [];
        }

        var accessor = model.Accessors[accessorIndex.Value];
        var bufferView = model.BufferViews[accessor.BufferView];

        var bufferData = bufferDataList[bufferView.Buffer];
        var offset = accessor.ByteOffset + bufferView.ByteOffset;
        var length = accessor.Count * GetComponentSize(accessor.ComponentType) * GetNumComponents(accessor.Type);

        if (offset + length > bufferData.Length)
        {
            return [];
        }

        var result = new byte[length];
        Array.Copy(bufferData, offset, result, 0, length);

        return result;
    }

    private static byte[] LoadBufferData(GltfBuffer buffer, string? basePath)
    {
        if (buffer.Uri == null)
        {
            return [];
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
