using System;

namespace Gnosis.Core.Platform;

/// <summary>
/// 提供系统信息查询功能
/// </summary>
public static class SystemInfo
{
    /// <summary>
    /// 获取处理器核心数量
    /// </summary>
    public static int ProcessorCount { get; } = Environment.ProcessorCount;

    /// <summary>
    /// 获取物理内存总量（字节）
    /// </summary>
    public static long TotalPhysicalMemory { get; } = DetectTotalPhysicalMemory();

    /// <summary>
    /// 获取可用内存大小（字节）
    /// </summary>
    public static long AvailableMemory { get; } = DetectAvailableMemory();

    /// <summary>
    /// 获取屏幕宽度（像素）
    /// </summary>
    public static int ScreenWidth { get; } = DetectScreenWidth();

    /// <summary>
    /// 获取屏幕高度（像素）
    /// </summary>
    public static int ScreenHeight { get; } = DetectScreenHeight();

    /// <summary>
    /// 获取屏幕 DPI
    /// </summary>
    public static int ScreenDpi { get; } = 96;

    /// <summary>
    /// 获取设备型号
    /// </summary>
    public static string DeviceModel { get; } = "Unknown";

    /// <summary>
    /// 获取图形设备名称
    /// </summary>
    public static string GraphicsDeviceName { get; } = "Unknown";

    /// <summary>
    /// 获取图形内存大小（字节）
    /// </summary>
    public static long GraphicsMemorySize { get; } = 0;

    /// <summary>
    /// 检测物理内存总量
    /// </summary>
    private static long DetectTotalPhysicalMemory()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            return gcInfo.TotalAvailableMemoryBytes;
        }
        catch (PlatformNotSupportedException)
        {
            return 0;
        }
    }

    /// <summary>
    /// 检测可用内存大小
    /// </summary>
    private static long DetectAvailableMemory()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var total = gcInfo.TotalAvailableMemoryBytes;
            var heapSize = gcInfo.HeapSizeBytes;
            var available = total - heapSize;

            return available > 0 ? available : 0;
        }
        catch (PlatformNotSupportedException)
        {
            return 0;
        }
    }

    /// <summary>
    /// 检测屏幕宽度
    /// </summary>
    private static int DetectScreenWidth()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                return Console.WindowWidth;
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }

        return 1920;
    }

    /// <summary>
    /// 检测屏幕高度
    /// </summary>
    private static int DetectScreenHeight()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                return Console.WindowHeight;
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }

        return 1080;
    }
}
