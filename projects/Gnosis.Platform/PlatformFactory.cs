using Gnosis.Core.Platform;
using Gnosis.Platform.Android;
using Gnosis.Platform.Handheld;
using Gnosis.Platform.iOS;
using Gnosis.Platform.Linux;
using Gnosis.Platform.MacOS;
using Gnosis.Platform.Web;
using Gnosis.Platform.Win32;

namespace Gnosis.Platform;

public static class PlatformFactory
{
    public static IPlatformWindow CreateWindow(WindowCreateInfo info)
    {
        var platform = PlatformInfo.CurrentPlatform;

        return platform switch
        {
            PlatformType.Windows => Win32Window.Create(info),
            PlatformType.Linux => Linux.X11Window.Create(info),
            PlatformType.macOS => MacOS.CocoaWindow.Create(info),
            PlatformType.WebAssembly => Web.WebWindow.Create(info),
            PlatformType.Android => Android.AndroidWindow.Create(info),
            PlatformType.iOS => iOS.IosWindow.Create(info),
            PlatformType.PlayStation => Handheld.ConsoleWindow.Create(info),
            PlatformType.Xbox => Handheld.ConsoleWindow.Create(info),
            PlatformType.Switch => Handheld.ConsoleWindow.Create(info),
            _ => throw new PlatformNotSupportedException($"不支持的平台: {platform}")
        };
    }
}
