namespace Gnosis.Assets.Formats.MeshOptimization;

/// <summary>
/// 法线生成器，通过面法线平均计算顶点法线
/// </summary>
public static class NormalGenerator
{
    #region 公开方法

    /// <summary>
    /// 为网格顶点生成法线
    /// 对缺少法线的顶点，通过面法线平均计算
    /// </summary>
    /// <param name="vertices">顶点列表</param>
    /// <param name="indices">索引缓冲区</param>
    /// <returns>每个顶点的法线数组</returns>
    public static float[][] Generate(IReadOnlyList<MeshVertex> vertices, IReadOnlyList<int> indices)
    {
        var vertexCount = vertices.Count;
        var normals = new float[vertexCount][];

        for (var i = 0; i < vertexCount; i++)
        {
            normals[i] = vertices[i].Normal is { Length: >= 3 }
                ? (float[])vertices[i].Normal.Clone()
                : new float[3];
        }

        var triangleCount = indices.Count / 3;

        for (var tri = 0; tri < triangleCount; tri++)
        {
            var i0 = indices[tri * 3];
            var i1 = indices[tri * 3 + 1];
            var i2 = indices[tri * 3 + 2];

            var p0 = vertices[i0].Position;
            var p1 = vertices[i1].Position;
            var p2 = vertices[i2].Position;

            if (p0.Length < 3 || p1.Length < 3 || p2.Length < 3)
            {
                continue;
            }

            var e1x = p1[0] - p0[0];
            var e1y = p1[1] - p0[1];
            var e1z = p1[2] - p0[2];

            var e2x = p2[0] - p0[0];
            var e2y = p2[1] - p0[1];
            var e2z = p2[2] - p0[2];

            var nx = e1y * e2z - e1z * e2y;
            var ny = e1z * e2x - e1x * e2z;
            var nz = e1x * e2y - e1y * e2x;

            var lengthSq = nx * nx + ny * ny + nz * nz;
            if (lengthSq < 1e-10f)
            {
                continue;
            }

            var invLength = 1.0f / MathF.Sqrt(lengthSq);
            nx *= invLength;
            ny *= invLength;
            nz *= invLength;

            normals[i0][0] += nx;
            normals[i0][1] += ny;
            normals[i0][2] += nz;

            normals[i1][0] += nx;
            normals[i1][1] += ny;
            normals[i1][2] += nz;

            normals[i2][0] += nx;
            normals[i2][1] += ny;
            normals[i2][2] += nz;
        }

        for (var i = 0; i < vertexCount; i++)
        {
            var x = normals[i][0];
            var y = normals[i][1];
            var z = normals[i][2];

            var lengthSq = x * x + y * y + z * z;
            if (lengthSq > 1e-10f)
            {
                var invLength = 1.0f / MathF.Sqrt(lengthSq);
                normals[i][0] *= invLength;
                normals[i][1] *= invLength;
                normals[i][2] *= invLength;
            }
            else
            {
                normals[i][0] = 0.0f;
                normals[i][1] = 1.0f;
                normals[i][2] = 0.0f;
            }
        }

        return normals;
    }

    #endregion
}
