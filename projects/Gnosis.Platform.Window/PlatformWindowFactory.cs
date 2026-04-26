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
        var os = Platform.Current.OS;

        return os switch
        {
            PlatformOS.Windows => Win32Window.Create(info),
            PlatformOS.Linux => X11Window.Create(info),
            PlatformOS.macOS => CocoaWindow.Create(info),
            PlatformOS.Web => WebWindow.Create(info),
            PlatformOS.Android => AndroidWindow.Create(info),
            PlatformOS.iOS => IosWindow.Create(info),
            PlatformOS.PlayStation => ConsoleWindow.Create(info),
            PlatformOS.Xbox => ConsoleWindow.Create(info),
            PlatformOS.Switch => ConsoleWindow.Create(info),
            _ => throw new PlatformNotSupportedException($"不支持的操作系统: {os}")
        };
    }
}
