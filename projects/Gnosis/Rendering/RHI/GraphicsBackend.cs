namespace Gnosis.Rendering.RHI;

/// <summary>
/// 图形后端类型枚举，标识底层 GPU API
/// </summary>
public enum GraphicsBackend
{
    /// <summary>
    /// 软件渲染，无 GPU 加速
    /// </summary>
    Software = 0,

    /// <summary>
    /// Vulkan 跨平台图形 API
    /// </summary>
    Vulkan = 1,

    /// <summary>
    /// Metal 苹果平台图形 API
    /// </summary>
    Metal = 2,

    /// <summary>
    /// Direct3D 12 Windows 平台图形 API
    /// </summary>
    D3D12 = 3
}
