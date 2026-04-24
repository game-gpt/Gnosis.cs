using Gnosis.Graphic.RHI.OpenGL;
using Gnosis.Graphic.RHI.Vulkan;

namespace Gnosis.Graphic.RHI;

public static class DeviceFactory
{
    public static IDevice Create(GraphicsBackend backend)
    {
        return backend switch
        {
            GraphicsBackend.OpenGL => new OpenGLDevice(),
            GraphicsBackend.Vulkan => new VulkanDevice(),
            GraphicsBackend.Metal => throw new NotSupportedException("Metal 后端尚未实现"),
            GraphicsBackend.D3D12 => throw new NotSupportedException("D3D12 后端尚未实现"),
            _ => throw new NotSupportedException($"不支持的图形后端类型：{backend}")
        };
    }

    public static GraphicsBackend DetectBestBackend()
    {
        if (OperatingSystem.IsWindows())
        {
            return GraphicsBackend.Vulkan;
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            return GraphicsBackend.Metal;
        }

        return GraphicsBackend.OpenGL;
    }
}
