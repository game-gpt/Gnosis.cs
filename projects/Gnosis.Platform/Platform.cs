using System.Runtime.InteropServices;

namespace Gnosis.Platform;

/// <summary>
/// 平台 = OS × ISA × Channel
/// 三个正交维度，编译期确定
/// </summary>
public sealed record Platform
{
    /// <summary>
    /// 操作系统：决定系统调用、窗口系统、图形 API
    /// </summary>
    public required PlatformOS OS { get; init; }

    /// <summary>
    /// 指令集架构：决定二进制格式、JIT 后端、原生库加载
    /// </summary>
    public required PlatformISA ISA { get; init; }

    /// <summary>
    /// 分发渠道：决定渠道 SDK 能力，插件可扩展
    /// </summary>
    public PlatformChannel Channel { get; init; } = PlatformChannel.Default;

    /// <summary>
    /// 平台显示名称
    /// Default 渠道："OS.ISA"（如 "Windows.X64"）
    /// 非 Default 渠道："Channel (OS.ISA)"（如 "Steam (Windows.X64)"）
    /// </summary>
    public string DisplayName => Channel == PlatformChannel.Default
        ? $"{OS}.{ISA}"
        : $"{Channel} ({OS}.{ISA})";

    /// <summary>
    /// 编译期确定的当前平台
    /// OS 通过编译常量确定：WINDOWS / LINUX / MACOS / ANDROID / IOS / WEB / PLAYSTATION / XBOX / SWITCH
    /// ISA 通过编译常量确定：X64 / ARM64 / WASM（未定义时从 OS 推断）
    /// Channel 默认为 Default，可通过 CHANNEL_STEAM / CHANNEL_WECHAT 等常量指定
    /// </summary>
    public static Platform Current { get; } = new Platform
    {
        OS = CurrentOS,
        ISA = CurrentISA,
        Channel = CurrentChannel
    };

    #region 编译期 OS

    private static PlatformOS CurrentOS =>
#if WINDOWS
        PlatformOS.Windows;
#elif LINUX
        PlatformOS.Linux;
#elif MACOS
        PlatformOS.macOS;
#elif ANDROID
        PlatformOS.Android;
#elif IOS
        PlatformOS.iOS;
#elif WEB
        PlatformOS.Web;
#elif PLAYSTATION
        PlatformOS.PlayStation;
#elif XBOX
        PlatformOS.Xbox;
#elif SWITCH
        PlatformOS.Switch;
#else
        DetectOSFallback();
#endif

    private static PlatformOS DetectOSFallback()
    {
        if (RuntimeInformation.FrameworkDescription.Contains("Browser"))
        {
            return PlatformOS.Web;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return PlatformOS.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return PlatformOS.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return PlatformOS.macOS;
        }

        return PlatformOS.Windows;
    }

    #endregion

    #region 编译期 ISA

#if X64
    private static PlatformISA CurrentISA => PlatformISA.X64;
#elif ARM64
    private static PlatformISA CurrentISA => PlatformISA.Arm64;
#elif WASM
    private static PlatformISA CurrentISA => PlatformISA.Wasm;
#else
    private static PlatformISA CurrentISA => InferISA(CurrentOS);
#endif

    /// <summary>
    /// 从 OS 推断 ISA：大多数平台 ISA 是固定的
    /// Windows/Linux/macOS 默认 X64，Android/iOS 默认 Arm64，Web 默认 Wasm
    /// 主机平台 ISA 固定
    /// </summary>
    private static PlatformISA InferISA(PlatformOS os)
    {
        return os switch
        {
            PlatformOS.Web => PlatformISA.Wasm,
            PlatformOS.iOS => PlatformISA.Arm64,
            PlatformOS.Android => PlatformISA.Arm64,
            PlatformOS.Switch => PlatformISA.Arm64,
            PlatformOS.PlayStation => PlatformISA.X64,
            PlatformOS.Xbox => PlatformISA.X64,
            _ => InferDesktopISA()
        };
    }

    private static PlatformISA InferDesktopISA()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        return arch == global::System.Runtime.InteropServices.Architecture.Arm64
            ? PlatformISA.Arm64
            : PlatformISA.X64;
    }

    #endregion

    #region 编译期 Channel

#if CHANNEL_STEAM
    private static PlatformChannel CurrentChannel => PlatformChannel.Steam;
#elif CHANNEL_WECHAT
    private static PlatformChannel CurrentChannel => PlatformChannel.WeChat;
#elif CHANNEL_EPICGAMES
    private static PlatformChannel CurrentChannel => PlatformChannel.EpicGames;
#elif CHANNEL_APPLEAPPSTORE
    private static PlatformChannel CurrentChannel => PlatformChannel.AppleAppStore;
#elif CHANNEL_GOOGLEPLAY
    private static PlatformChannel CurrentChannel => PlatformChannel.GooglePlay;
#else
    private static PlatformChannel CurrentChannel => PlatformChannel.Default;
#endif

    #endregion

    public override string ToString() => DisplayName;
}
