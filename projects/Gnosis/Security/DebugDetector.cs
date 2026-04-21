using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gnosis.Security;

/// <summary>
/// 调试器检测器，用于检测当前进程是否正在被调试。
/// </summary>
public static class DebugDetector
{
    /// <summary>
    /// 检测当前进程是否正在被调试器附加。
    /// </summary>
    /// <returns>如果任意检测方式发现调试器存在，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
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

    /// <summary>
    /// 通过 P/Invoke 调用 Windows kernel32.dll 的 IsDebuggerPresent 函数检测调试器。
    /// </summary>
    /// <returns>如果调试器存在则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresentNative();
}
