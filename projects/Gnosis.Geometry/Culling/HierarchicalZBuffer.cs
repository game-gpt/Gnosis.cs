using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Geometry.Culling;

/// <summary>
/// 层次 Z-Buffer（Hierarchical Z-Buffer / HZB）
/// 通过深度缓冲区的 Mip 链实现高效的遮挡剔除
/// 每一级 Mip 存储上一级 Mip 的最大深度值，形成从细到粗的深度层次结构
/// </summary>
public sealed class HierarchicalZBuffer
{
    #region 属性

    /// <summary>
    /// 深度缓冲区宽度
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// 深度缓冲区高度
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Mip 级别数量
    /// </summary>
    public int MipLevels => _mipLevels.Length;

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public bool IsInitialized => _mipLevels.Length > 0;

    #endregion

    #region 字段

    private float[][] _mipLevels = [];

    #endregion

    #region 公开方法

    /// <summary>
    /// 从深度缓冲区构建 HZB
    /// </summary>
    /// <param name="depthBuffer">深度缓冲区（行优先，值范围 [0, 1]）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public void Build(ReadOnlySpan<float> depthBuffer, int width, int height)
    {
        Width = width;
        Height = height;

        var mipCount = ComputeMipLevels(width, height);
        _mipLevels = new float[mipCount][];

        _mipLevels[0] = new float[width * height];
        depthBuffer.CopyTo(_mipLevels[0]);

        var currentWidth = width;
        var currentHeight = height;

        for (var level = 1; level < mipCount; level++)
        {
            var halfWidth = Math.Max(1, currentWidth / 2);
            var halfHeight = Math.Max(1, currentHeight / 2);

            _mipLevels[level] = new float[halfWidth * halfHeight];

            BuildMipLevel(_mipLevels[level - 1], currentWidth, currentHeight,
                _mipLevels[level], halfWidth, halfHeight);

            currentWidth = halfWidth;
            currentHeight = halfHeight;
        }
    }

    /// <summary>
    /// 使用遮挡查询判断一个屏幕空间包围盒是否被遮挡
    /// </summary>
    /// <param name="screenMinX">屏幕空间最小 X（像素）</param>
    /// <param name="screenMinY">屏幕空间最小 Y（像素）</param>
    /// <param name="screenMaxX">屏幕空间最大 X（像素）</param>
    /// <param name="screenMaxY">屏幕空间最大 Y（像素）</param>
    /// <param name="depthMin">物体的最近深度值</param>
    /// <returns>true 表示被遮挡（不可见），false 表示可见</returns>
    public bool IsOccluded(float screenMinX, float screenMinY, float screenMaxX, float screenMaxY, float depthMin)
    {
        if (!IsInitialized)
        {
            return false;
        }

        var level = SelectMipLevel(screenMinX, screenMinY, screenMaxX, screenMaxY);
        if (level < 0 || level >= _mipLevels.Length)
        {
            return false;
        }

        var mip = _mipLevels[level];
        var mipWidth = Math.Max(1, Width >> level);
        var mipHeight = Math.Max(1, Height >> level);

        var uMin = Math.Clamp((int)(screenMinX / Width * mipWidth), 0, mipWidth - 1);
        var vMin = Math.Clamp((int)(screenMinY / Height * mipHeight), 0, mipHeight - 1);
        var uMax = Math.Clamp((int)(screenMaxX / Width * mipWidth), 0, mipWidth - 1);
        var vMax = Math.Clamp((int)(screenMaxY / Height * mipHeight), 0, mipHeight - 1);

        var maxDepth = float.MinValue;

        for (var v = vMin; v <= vMax; v++)
        {
            for (var u = uMin; u <= uMax; u++)
            {
                var depth = mip[v * mipWidth + u];
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }
        }

        return depthMin > maxDepth;
    }

    /// <summary>
    /// 使用世界空间包围盒和相机参数进行遮挡查询
    /// </summary>
    /// <param name="worldBounds">世界空间包围盒</param>
    /// <param name="viewProjection">视图投影矩阵</param>
    /// <returns>true 表示被遮挡（不可见），false 表示可见</returns>
    public bool IsOccluded(BoundingBox worldBounds, Matrix4x4 viewProjection)
    {
        if (!IsInitialized)
        {
            return false;
        }

        var corners = GetBoundingBoxCorners(worldBounds);

        var screenMinX = float.MaxValue;
        var screenMinY = float.MaxValue;
        var screenMaxX = float.MinValue;
        var screenMaxY = float.MinValue;
        var depthMin = float.MaxValue;

        foreach (var corner in corners)
        {
            var clipPos = Vector4.Transform(new Vector4(corner, 1.0f), viewProjection);

            if (clipPos.W <= 0.0f)
            {
                return false;
            }

            var ndcX = clipPos.X / clipPos.W;
            var ndcY = clipPos.Y / clipPos.W;
            var ndcZ = clipPos.Z / clipPos.W;

            var screenX = (ndcX * 0.5f + 0.5f) * Width;
            var screenY = (1.0f - (ndcY * 0.5f + 0.5f)) * Height;
            var depth = ndcZ;

            screenMinX = Math.Min(screenMinX, screenX);
            screenMinY = Math.Min(screenMinY, screenY);
            screenMaxX = Math.Max(screenMaxX, screenX);
            screenMaxY = Math.Max(screenMaxY, screenY);
            depthMin = Math.Min(depthMin, depth);
        }

        if (screenMaxX < 0 || screenMaxY < 0 || screenMinX >= Width || screenMinY >= Height)
        {
            return true;
        }

        return IsOccluded(screenMinX, screenMinY, screenMaxX, screenMaxY, depthMin);
    }

    /// <summary>
    /// 获取指定 Mip 级别的深度数据
    /// </summary>
    public ReadOnlySpan<float> GetMipLevel(int level)
    {
        if (level < 0 || level >= _mipLevels.Length)
        {
            return [];
        }

        return _mipLevels[level];
    }

    /// <summary>
    /// 获取指定 Mip 级别的尺寸
    /// </summary>
    public (int Width, int Height) GetMipLevelSize(int level)
    {
        if (level < 0 || level >= _mipLevels.Length)
        {
            return (0, 0);
        }

        return (Math.Max(1, Width >> level), Math.Max(1, Height >> level));
    }

    /// <summary>
    /// 重置 HZB
    /// </summary>
    public void Reset()
    {
        _mipLevels = [];
        Width = 0;
        Height = 0;
    }

    #endregion

    #region 私有方法

    private static int ComputeMipLevels(int width, int height)
    {
        var maxDim = Math.Max(width, height);
        var levels = 0;

        while (maxDim > 0)
        {
            levels++;
            maxDim >>= 1;
        }

        return Math.Max(1, levels);
    }

    private static void BuildMipLevel(
        float[] srcMip, int srcWidth, int srcHeight,
        float[] dstMip, int dstWidth, int dstHeight)
    {
        for (var y = 0; y < dstHeight; y++)
        {
            for (var x = 0; x < dstWidth; x++)
            {
                var srcX = x * 2;
                var srcY = y * 2;

                var maxDepth = float.MinValue;

                for (var dy = 0; dy < 2; dy++)
                {
                    for (var dx = 0; dx < 2; dx++)
                    {
                        var sx = Math.Min(srcX + dx, srcWidth - 1);
                        var sy = Math.Min(srcY + dy, srcHeight - 1);

                        var depth = srcMip[sy * srcWidth + sx];
                        if (depth > maxDepth)
                        {
                            maxDepth = depth;
                        }
                    }
                }

                dstMip[y * dstWidth + x] = maxDepth;
            }
        }
    }

    private int SelectMipLevel(float screenMinX, float screenMinY, float screenMaxX, float screenMaxY)
    {
        var screenWidth = screenMaxX - screenMinX;
        var screenHeight = screenMaxY - screenMinY;

        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return 0;
        }

        var maxScreenDim = Math.Max(screenWidth, screenHeight);
        var level = 0;

        while (maxScreenDim > 2.0f && level < _mipLevels.Length - 1)
        {
            maxScreenDim *= 0.5f;
            level++;
        }

        return level;
    }

    private static Vector3[] GetBoundingBoxCorners(BoundingBox box)
    {
        return
        [
            new Vector3(box.Min.X, box.Min.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Min.Z),
            new Vector3(box.Min.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Min.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Min.X, box.Max.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Max.Z)
        ];
    }

    #endregion
}
