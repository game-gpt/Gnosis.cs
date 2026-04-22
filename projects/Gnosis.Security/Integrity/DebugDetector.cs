using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gnosis.Security.Integrity;

public static class DebugDetector
{
    public static bool IsDebuggerPresent()
    {
        if (Debugger.IsAttached)
        {
            return true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IsDebuggerPresentNative();
        }

        return false;
    }

    [DllImport("kernel32.dll", EntryPoint = "IsDebuggerPresent")]
    private static extern bool IsDebuggerPresentNative();
}
