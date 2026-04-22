namespace Gnosis.Asset.Format.MeshParsers;

public sealed class ObjParser
{
    public ObjParseResult Parse(ReadOnlySpan<char> content, string fileName = "")
    {
        var positions = new List<float[]>();
        var normals = new List<float[]>();
        var uvs = new List<float[]>();
        var faces = new List<ObjFace>();
        var subMeshNames = new List<string>();
        var subMeshFaceRanges = new List<(int Start, int Count)>();
        var currentSubMeshName = fileName;
        var currentFaceStart = 0;

        while (!content.IsEmpty)
        {
            var lineEnd = content.IndexOf('\n');
            ReadOnlySpan<char> line;

            if (lineEnd >= 0)
            {
                line = content[..lineEnd].Trim();
                content = content[(lineEnd + 1)..];
            }
            else
            {
                line = content.Trim();
                content = ReadOnlySpan<char>.Empty;
            }

            if (line.IsEmpty || line[0] == '#')
            {
                continue;
            }

            var keywordEnd = line.IndexOf(' ');
            if (keywordEnd < 0)
            {
                continue;
            }

            var keyword = line[..keywordEnd];
            var rest = line[(keywordEnd + 1)..].TrimStart();

            if (keyword.SequenceEqual("v"))
            {
                ParseVertex(rest, positions);
            }
            else if (keyword.SequenceEqual("vn"))
            {
                ParseNormal(rest, normals);
            }
            else if (keyword.SequenceEqual("vt"))
            {
                ParseUv(rest, uvs);
            }
            else if (keyword.SequenceEqual("f"))
            {
                ParseFace(rest, faces);
            }
            else if (keyword.SequenceEqual("o") || keyword.SequenceEqual("g"))
            {
                if (faces.Count > currentFaceStart)
                {
                    subMeshNames.Add(currentSubMeshName);
                    subMeshFaceRanges.Add((currentFaceStart, faces.Count - currentFaceStart));
                }

                currentSubMeshName = rest.ToString();
                currentFaceStart = faces.Count;
            }
        }

        if (faces.Count > currentFaceStart)
        {
            subMeshNames.Add(currentSubMeshName);
            subMeshFaceRanges.Add((currentFaceStart, faces.Count - currentFaceStart));
        }

        if (subMeshNames.Count == 0 && faces.Count > 0)
        {
            subMeshNames.Add(currentSubMeshName);
            subMeshFaceRanges.Add((0, faces.Count));
        }

        return BuildMeshData(positions, normals, uvs, faces, subMeshNames, subMeshFaceRanges);
    }

    private static void ParseVertex(ReadOnlySpan<char> rest, List<float[]> positions)
    {
        var parts = SplitBySpace(rest);
        if (parts.Count >= 3)
        {
            var pos = new float[3];
            pos[0] = float.TryParse(parts[0], out var x) ? x : 0f;
            pos[1] = float.TryParse(parts[1], out var y) ? y : 0f;
            pos[2] = float.TryParse(parts[2], out var z) ? z : 0f;
            positions.Add(pos);
        }
    }

    private static void ParseNormal(ReadOnlySpan<char> rest, List<float[]> normals)
    {
        var parts = SplitBySpace(rest);
        if (parts.Count >= 3)
        {
            var normal = new float[3];
            normal[0] = float.TryParse(parts[0], out var x) ? x : 0f;
            normal[1] = float.TryParse(parts[1], out var y) ? y : 0f;
            normal[2] = float.TryParse(parts[2], out var z) ? z : 0f;
            normals.Add(normal);
        }
    }

    private static void ParseUv(ReadOnlySpan<char> rest, List<float[]> uvs)
    {
        var parts = SplitBySpace(rest);
        if (parts.Count >= 2)
        {
            var uv = new float[2];
            uv[0] = float.TryParse(parts[0], out var u) ? u : 0f;
            uv[1] = float.TryParse(parts[1], out var v) ? v : 0f;
            uvs.Add(uv);
        }
    }

    private static void ParseFace(ReadOnlySpan<char> rest, List<ObjFace> faces)
    {
        var parts = SplitBySpace(rest);
        if (parts.Count < 3)
        {
            return;
        }

        var faceVertices = new List<ObjFaceVertex>(parts.Count);

        for (var i = 0; i < parts.Count; i++)
        {
            faceVertices.Add(ParseFaceVertex(parts[i]));
        }

        for (var i = 1; i < faceVertices.Count - 1; i++)
        {
            faces.Add(new ObjFace
            {
                V0 = faceVertices[0],
                V1 = faceVertices[i],
                V2 = faceVertices[i + 1]
            });
        }
    }

    private static ObjFaceVertex ParseFaceVertex(ReadOnlySpan<char> part)
    {
        var vertex = new ObjFaceVertex();
        var slash1 = part.IndexOf('/');

        if (slash1 < 0)
        {
            vertex.PositionIndex = int.TryParse(part, out var v) ? v : 0;
            return vertex;
        }

        if (slash1 > 0)
        {
            var vSpan = part[..slash1];
            vertex.PositionIndex = int.TryParse(vSpan, out var v) ? v : 0;
        }

        var afterSlash1 = part[(slash1 + 1)..];
        var slash2 = afterSlash1.IndexOf('/');

        if (slash2 < 0)
        {
            if (!afterSlash1.IsEmpty)
            {
                vertex.UvIndex = int.TryParse(afterSlash1, out var vt) ? vt : 0;
            }

            return vertex;
        }

        if (slash2 > 0)
        {
            var vtSpan = afterSlash1[..slash2];
            vertex.UvIndex = int.TryParse(vtSpan, out var vt) ? vt : 0;
        }

        var afterSlash2 = afterSlash1[(slash2 + 1)..];

        if (!afterSlash2.IsEmpty)
        {
            vertex.NormalIndex = int.TryParse(afterSlash2, out var vn) ? vn : 0;
        }

        return vertex;
    }

    private static ObjParseResult BuildMeshData(
        List<float[]> positions,
        List<float[]> normals,
        List<float[]> uvs,
        List<ObjFace> faces,
        List<string> subMeshNames,
        List<(int Start, int Count)> subMeshFaceRanges)
    {
        var vertexMap = new Dictionary<ObjFaceVertex, int>();
        var vertices = new List<MeshVertex>();
        var indices = new List<int>();

        foreach (var face in faces)
        {
            AddVertex(face.V0, positions, normals, uvs, vertexMap, vertices, indices);
            AddVertex(face.V1, positions, normals, uvs, vertexMap, vertices, indices);
            AddVertex(face.V2, positions, normals, uvs, vertexMap, vertices, indices);
        }

        var subMeshes = new List<MeshSubMesh>();

        foreach (var (name, range) in subMeshNames.Zip(subMeshFaceRanges))
        {
            subMeshes.Add(new MeshSubMesh
            {
                IndexStart = range.Start * 3,
                IndexCount = range.Count * 3,
                MaterialPath = string.Empty
            });
        }

        var bounds = ComputeBounds(vertices);

        return new ObjParseResult
        {
            Vertices = vertices,
            Indices = indices,
            SubMeshes = subMeshes,
            SubMeshNames = subMeshNames,
            Bounds = bounds
        };
    }

    private static void AddVertex(
        ObjFaceVertex faceVertex,
        List<float[]> positions,
        List<float[]> normals,
        List<float[]> uvs,
        Dictionary<ObjFaceVertex, int> vertexMap,
        List<MeshVertex> vertices,
        List<int> indices)
    {
        if (vertexMap.TryGetValue(faceVertex, out var existingIndex))
        {
            indices.Add(existingIndex);
            return;
        }

        var meshVertex = new MeshVertex();

        var posIdx = ResolveIndex(faceVertex.PositionIndex, positions.Count);
        if (posIdx >= 0 && posIdx < positions.Count)
        {
            meshVertex = meshVertex with { Position = positions[posIdx] };
        }

        if (faceVertex.NormalIndex != 0)
        {
            var normIdx = ResolveIndex(faceVertex.NormalIndex, normals.Count);
            if (normIdx >= 0 && normIdx < normals.Count)
            {
                meshVertex = meshVertex with { Normal = normals[normIdx] };
            }
        }

        if (faceVertex.UvIndex != 0)
        {
            var uvIdx = ResolveIndex(faceVertex.UvIndex, uvs.Count);
            if (uvIdx >= 0 && uvIdx < uvs.Count)
            {
                meshVertex = meshVertex with { Uv = uvs[uvIdx] };
            }
        }

        var newIndex = vertices.Count;
        vertices.Add(meshVertex);
        vertexMap[faceVertex] = newIndex;
        indices.Add(newIndex);
    }

    private static int ResolveIndex(int index, int count)
    {
        if (index > 0)
        {
            return index - 1;
        }

        if (index < 0)
        {
            return count + index;
        }

        return -1;
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

    private static List<string> SplitBySpace(ReadOnlySpan<char> span)
    {
        var result = new List<string>();

        while (!span.IsEmpty)
        {
            while (!span.IsEmpty && span[0] == ' ')
            {
                span = span[1..];
            }

            if (span.IsEmpty)
            {
                break;
            }

            var end = span.IndexOf(' ');
            if (end < 0)
            {
                result.Add(span.ToString());
                break;
            }

            result.Add(span[..end].ToString());
            span = span[(end + 1)..];
        }

        return result;
    }
}

public struct ObjFaceVertex : IEquatable<ObjFaceVertex>
{
    public int PositionIndex;
    public int UvIndex;
    public int NormalIndex;

    public bool Equals(ObjFaceVertex other)
    {
        return PositionIndex == other.PositionIndex
               && UvIndex == other.UvIndex
               && NormalIndex == other.NormalIndex;
    }

    public override bool Equals(object? obj)
    {
        return obj is ObjFaceVertex other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PositionIndex, UvIndex, NormalIndex);
    }
}

public struct ObjFace
{
    public ObjFaceVertex V0;
    public ObjFaceVertex V1;
    public ObjFaceVertex V2;
}

public class ObjParseResult
{
    public List<MeshVertex> Vertices { get; init; } = [];
    public List<int> Indices { get; init; } = [];
    public List<MeshSubMesh> SubMeshes { get; init; } = [];
    public List<string> SubMeshNames { get; init; } = [];
    public MeshBounds Bounds { get; init; } = new();
}
