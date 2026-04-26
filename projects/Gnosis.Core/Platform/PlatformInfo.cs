using System;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Platform;

/// <summary>
/// 提供运行时平台检测功能（已过时）
/// 平台身份请使用 Gnosis.Platform.Platform.Current 替代
/// 系统信息查询功能保留在此类中
/// </summary>
public static class PlatformInfo
{
    /// <summary>
    /// 获取当前运行平台类型（已过时）
    /// 请使用 Gnosis.Platform.Platform.Current.Architecture 替代
    /// </summary>
    [Obsolete("请使用 Gnosis.Platform.Platform.Current.Architecture 替代。平台应在编译期确定")]
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

    [Obsolete("平台应在编译期确定，请使用 Gnosis.Platform.Platform.Current")]
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

    private static bool DetectMobile()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
        {
            return true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == global::System.Runtime.InteropServices.Architecture.Arm64)
        {
            return true;
        }

        return false;
    }

    private static bool DetectConsole()
    {
        return false;
    }
}
