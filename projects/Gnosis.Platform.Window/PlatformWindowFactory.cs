using Gnosis.Core.Platform;
using Gnosis.Platform.Window.Android;
using Gnosis.Platform.Window.Handheld;
using Gnosis.Platform.Window.iOS;
using Gnosis.Platform.Window.Linux;
using Gnosis.Platform.Window.MacOS;
using Gnosis.Platform.Window.Web;
using Gnosis.Platform.Window.Win32;

namespace Gnosis.Platform.Window;

public static class PlatformWindowFactory
{
    public static IPlatformWindow Create(WindowCreateInfo info)
    {
        var platform = PlatformInfo.CurrentPlatform;

        return platform switch
        {
            PlatformType.Windows => Win32Window.Create(info),
            PlatformType.Linux => X11Window.Create(info),
            PlatformType.macOS => CocoaWindow.Create(info),
            PlatformType.WebAssembly => WebWindow.Create(info),
            PlatformType.Android => AndroidWindow.Create(info),
            PlatformType.iOS => IosWindow.Create(info),
            PlatformType.PlayStation => ConsoleWindow.Create(info),
            PlatformType.Xbox => ConsoleWindow.Create(info),
            PlatformType.Switch => ConsoleWindow.Create(info),
            _ => throw new PlatformNotSupportedException($"不支持的平台: {platform}")
        };
    }
}
