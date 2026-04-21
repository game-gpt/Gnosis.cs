namespace Gnosis.Assets.Formats.MeshOptimization;

/// <summary>
/// 切线生成器，基于 Eric Lengyel 方法计算切线空间
/// </summary>
public static class TangentGenerator
{
    #region 公开方法

    /// <summary>
    /// 为网格顶点生成切线
    /// 要求顶点必须包含 Position、Normal 和 UV 数据
    /// </summary>
    /// <param name="vertices">顶点列表</param>
    /// <param name="indices">索引缓冲区</param>
    /// <returns>每个顶点的切线数组（4 分量：xyz 为切线方向，w 为手性）</returns>
    public static float[][] Generate(IReadOnlyList<MeshVertex> vertices, IReadOnlyList<int> indices)
    {
        int vertexCount = vertices.Count;
        var tangentAccum = new float[vertexCount][];
        var bitangentAccum = new float[vertexCount][];

        for (int i = 0; i < vertexCount; i++)
        {
            tangentAccum[i] = new float[3];
            bitangentAccum[i] = new float[3];
        }

        int triangleCount = indices.Count / 3;

        for (int tri = 0; tri < triangleCount; tri++)
        {
            int i0 = indices[tri * 3];
            int i1 = indices[tri * 3 + 1];
            int i2 = indices[tri * 3 + 2];

            var p0 = vertices[i0].Position;
            var p1 = vertices[i1].Position;
            var p2 = vertices[i2].Position;

            var uv0 = vertices[i0].Uv;
            var uv1 = vertices[i1].Uv;
            var uv2 = vertices[i2].Uv;

            if (p0.Length < 3 || p1.Length < 3 || p2.Length < 3)
            {
                continue;
            }

            if (uv0.Length < 2 || uv1.Length < 2 || uv2.Length < 2)
            {
                continue;
            }

            float x1 = p1[0] - p0[0];
            float y1 = p1[1] - p0[1];
            float z1 = p1[2] - p0[2];

            float x2 = p2[0] - p0[0];
            float y2 = p2[1] - p0[1];
            float z2 = p2[2] - p0[2];

            float s1 = uv1[0] - uv0[0];
            float t1 = uv1[1] - uv0[1];

            float s2 = uv2[0] - uv0[0];
            float t2 = uv2[1] - uv0[1];

            float det = s1 * t2 - s2 * t1;

            float r = MathF.Abs(det) < 1e-10f ? 1.0f : 1.0f / det;

            float tx = (t2 * x1 - t1 * x2) * r;
            float ty = (t2 * y1 - t1 * y2) * r;
            float tz = (t2 * z1 - t1 * z2) * r;

            float bx = (s1 * x2 - s2 * x1) * r;
            float by = (s1 * y2 - s2 * y1) * r;
            float bz = (s1 * z2 - s2 * z1) * r;

            for (int j = 0; j < 3; j++)
            {
                int idx = indices[tri * 3 + j];
                tangentAccum[idx][0] += tx;
                tangentAccum[idx][1] += ty;
                tangentAccum[idx][2] += tz;
                bitangentAccum[idx][0] += bx;
                bitangentAccum[idx][1] += by;
                bitangentAccum[idx][2] += bz;
            }
        }

        var result = new float[vertexCount][];

        for (int i = 0; i < vertexCount; i++)
        {
            result[i] = new float[4];

            var normal = vertices[i].Normal;
            if (normal.Length < 3)
            {
                result[i][0] = 1.0f;
                result[i][1] = 0.0f;
                result[i][2] = 0.0f;
                result[i][3] = 1.0f;
                continue;
            }

            float nx = normal[0];
            float ny = normal[1];
            float nz = normal[2];

            float tx = tangentAccum[i][0];
            float ty = tangentAccum[i][1];
            float tz = tangentAccum[i][2];

            float dot = nx * tx + ny * ty + nz * tz;

            tx -= nx * dot;
            ty -= ny * dot;
            tz -= nz * dot;

            float lengthSq = tx * tx + ty * ty + tz * tz;
            if (lengthSq > 1e-10f)
            {
                float invLength = 1.0f / MathF.Sqrt(lengthSq);
                tx *= invLength;
                ty *= invLength;
                tz *= invLength;
            }
            else
            {
                tx = 1.0f;
                ty = 0.0f;
                tz = 0.0f;
            }

            float bx = bitangentAccum[i][0];
            float by = bitangentAccum[i][1];
            float bz = bitangentAccum[i][2];

            float nctX = ny * tz - nz * ty;
            float nctY = nz * tx - nx * tz;
            float nctZ = nx * ty - ny * tx;

            float handedness = (nctX * bx + nctY * by + nctZ * bz) < 0.0f ? -1.0f : 1.0f;

            result[i][0] = tx;
            result[i][1] = ty;
            result[i][2] = tz;
            result[i][3] = handedness;
        }

        return result;
    }

    #endregion
}
