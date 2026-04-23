using System;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Platform;

/// <summary>
/// 提供运行时平台检测功能
/// </summary>
public static class PlatformInfo
{
    /// <summary>
    /// 获取当前运行平台类型
    /// </summary>
    public static PlatformType CurrentPlatform { get; } = DetectPlatform();

    /// <summary>
    /// 获取操作系统版本信息
    /// </summary>
    public static string OSVersion { get; } = Environment.OSVersion.ToString();

    /// <summary>
    /// 获取当前进程的 CPU 架构
    /// </summary>
    public static SystemArchitecture Architecture { get; } = DetectArchitecture();

    /// <summary>
    /// 获取当前进程是否为 64 位
    /// </summary>
    public static bool Is64Bit { get; } = Environment.Is64BitProcess;

    /// <summary>
    /// 获取当前是否运行在 WebAssembly 环境中
    /// </summary>
    public static bool IsWasm { get; } = RuntimeInformation.FrameworkDescription.Contains("Browser");

    /// <summary>
    /// 获取当前是否运行在移动平台上
    /// </summary>
    public static bool IsMobile { get; } = DetectMobile();

    /// <summary>
    /// 获取当前是否运行在控制台平台上
    /// </summary>
    public static bool IsConsole { get; } = DetectConsole();

    /// <summary>
    /// 获取运行时框架版本信息
    /// </summary>
    public static string RuntimeVersion { get; } = RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// 检测当前运行平台类型
    /// </summary>
    private static PlatformType DetectPlatform()
    {
        if (RuntimeInformation.FrameworkDescription.Contains("Browser"))
        {
            return PlatformType.WebAssembly;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return PlatformType.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return PlatformType.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return PlatformType.macOS;
        }

        return PlatformType.Windows;
    }

    /// <summary>
    /// 检测当前 CPU 架构
    /// </summary>
    private static SystemArchitecture DetectArchitecture()
    {
        var arch = RuntimeInformation.ProcessArchitecture;

        if (arch == global::System.Runtime.InteropServices.Architecture.X64)
        {
            return SystemArchitecture.X64;
        }

        if (arch == global::System.Runtime.InteropServices.Architecture.X86)
        {
            return SystemArchitecture.X86;
        }

        if (arch == global::System.Runtime.InteropServices.Architecture.Arm64)
        {
            return SystemArchitecture.Arm64;
        }

        if (arch == global::System.Runtime.InteropServices.Architecture.Arm)
        {
            return SystemArchitecture.Arm;
        }

        if (arch == global::System.Runtime.InteropServices.Architecture.Wasm)
        {
            return SystemArchitecture.Wasm;
        }

        return SystemArchitecture.Unknown;
    }

    /// <summary>
    /// 检测当前是否为移动平台
    /// </summary>
    private static bool DetectMobile()
    {
        if (CurrentPlatform == PlatformType.iOS || CurrentPlatform == PlatformType.Android)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检测当前是否为控制台平台
    /// </summary>
    private static bool DetectConsole()
    {
        if (CurrentPlatform == PlatformType.PlayStation ||
            CurrentPlatform == PlatformType.Xbox ||
            CurrentPlatform == PlatformType.Switch)
        {
            return true;
        }

        return false;
    }
}
