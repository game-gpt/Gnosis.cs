using System.Numerics;

namespace Gnosis.Geometry.Lod;

/// <summary>
/// 屏幕空间误差计算器
/// 根据簇的包围信息和相机参数计算屏幕空间误差，用于动态 LOD 选择
/// </summary>
public static class ScreenSpaceError
{
    #region 公开方法

    /// <summary>
    /// 计算包围球在屏幕上的投影误差（像素）
    /// </summary>
    /// <param name="sphereCenter">世界空间包围球中心</param>
    /// <param name="sphereRadius">世界空间包围球半径</param>
    /// <param name="cameraPosition">相机世界位置</param>
    /// <param name="cameraForward">相机前方向</param>
    /// <param name="fovY">垂直视场角（弧度）</param>
    /// <param name="screenHeight">屏幕高度（像素）</param>
    /// <returns>屏幕空间误差（像素）</returns>
    public static float Calculate(
        Vector3 sphereCenter,
        float sphereRadius,
        Vector3 cameraPosition,
        Vector3 cameraForward,
        float fovY,
        float screenHeight)
    {
        var toCenter = sphereCenter - cameraPosition;
        var distance = Vector3.Dot(toCenter, cameraForward);

        if (distance <= 0.0f)
        {
            return float.MaxValue;
        }

        var projectedRadius = sphereRadius / distance;
        var screenError = projectedRadius * screenHeight / MathF.Tan(fovY * 0.5f);

        return screenError;
    }

    /// <summary>
    /// 计算包围盒在屏幕上的投影误差（像素）
    /// 取包围盒对角线的一半作为近似半径
    /// </summary>
    /// <param name="boundsMin">包围盒最小点</param>
    /// <param name="boundsMax">包围盒最大点</param>
    /// <param name="cameraPosition">相机世界位置</param>
    /// <param name="cameraForward">相机前方向</param>
    /// <param name="fovY">垂直视场角（弧度）</param>
    /// <param name="screenHeight">屏幕高度（像素）</param>
    /// <returns>屏幕空间误差（像素）</returns>
    public static float CalculateFromBounds(
        Vector3 boundsMin,
        Vector3 boundsMax,
        Vector3 cameraPosition,
        Vector3 cameraForward,
        float fovY,
        float screenHeight)
    {
        var center = (boundsMin + boundsMax) * 0.5f;
        var extents = (boundsMax - boundsMin) * 0.5f;
        var radius = extents.Length();

        return Calculate(center, radius, cameraPosition, cameraForward, fovY, screenHeight);
    }

    /// <summary>
    /// 判断给定屏幕空间误差是否应该切换到更精细的 LOD
    /// </summary>
    /// <param name="currentError">当前 LOD 的屏幕空间误差</param>
    /// <param name="threshold">LOD 切换阈值</param>
    /// <returns>true 表示需要切换到更精细的 LOD</returns>
    public static bool ShouldRefine(float currentError, float threshold)
    {
        return currentError > threshold;
    }

    /// <summary>
    /// 判断给定屏幕空间误差是否应该切换到更粗略的 LOD
    /// </summary>
    /// <param name="currentError">当前 LOD 的屏幕空间误差</param>
    /// <param name="threshold">LOD 切换阈值</param>
    /// <returns>true 表示需要切换到更粗略的 LOD</returns>
    public static bool ShouldCoarsen(float currentError, float threshold)
    {
        return currentError < threshold * 0.5f;
    }

    #endregion
}
