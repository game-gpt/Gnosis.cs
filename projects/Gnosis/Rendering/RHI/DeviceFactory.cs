namespace Gnosis.Rendering.RHI;

/// <summary>
/// 图形设备工厂，根据后端类型创建对应的 IDevice 实例
/// </summary>
public static class DeviceFactory
{
    /// <summary>
    /// 根据指定的后端类型创建图形设备
    /// </summary>
    /// <param name="backend">图形后端类型</param>
    /// <returns>图形设备实例</returns>
    /// <exception cref="NotSupportedException">不支持的后端类型</exception>
    public static IDevice Create(GraphicsBackend backend)
    {
        return backend switch
        {
            GraphicsBackend.Software => new SoftwareDevice(),
            GraphicsBackend.Vulkan => new VulkanDevice(),
            GraphicsBackend.Metal => new MetalDevice(),
            GraphicsBackend.D3D12 => new D3D12Device(),
            _ => throw new NotSupportedException($"不支持的图形后端类型：{backend}")
        };
    }

    /// <summary>
    /// 检测当前平台推荐的图形后端
    /// </summary>
    /// <returns>推荐的图形后端类型</returns>
    public static GraphicsBackend DetectBestBackend()
    {
        if (OperatingSystem.IsWindows())
        {
            return GraphicsBackend.D3D12;
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            return GraphicsBackend.Metal;
        }

        return GraphicsBackend.Vulkan;
    }
}
