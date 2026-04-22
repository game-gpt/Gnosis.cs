using System.Runtime.InteropServices;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Capture;

/// <summary>
/// RenderDoc 原生 API 绑定，通过 renderdoc.dll 动态加载
/// </summary>
internal static class RenderDocNative
{
    #region 常量

    private const string RenderDocDllName = "renderdoc.dll";
    private const int RenderDocApiVersion = 10600;

    #endregion

    #region P/Invoke 声明

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern nint GetProcAddress(nint hModule, string lpProcName);

    #endregion

    #region 函数指针类型

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_GetAPIDelegate(int version, out nint outAPI);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_TriggerCaptureDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_TriggerMultiFrameCaptureDelegate(int numFrames);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_StartFrameCaptureDelegate(nint device, nint wndHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_EndFrameCaptureDelegate(nint device, nint wndHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_SetCaptureFilePathTemplateDelegate([MarshalAs(UnmanagedType.LPStr)] string filePath);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_GetNumCapturesDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_GetCaptureDelegate(int idx, nint logfile, nint timestamp, nint pathLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_SetFocusToggleKeysDelegate(ref uint keys, int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_SetCaptureKeysDelegate(ref uint keys, int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint RENDERDOC_GetOverlayBitsDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_MaskOverlayBitsDelegate(uint andMask, uint orMask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RENDERDOC_IsTargetControlConnectedDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint RENDERDOC_LaunchReplayUIDelegate(int connectTargetControl, string cmdLine);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_SetActiveWindowDelegate(nint device, nint wndHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_StartDebugCaptureLogDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RENDERDOC_EndDebugCaptureLogDelegate();

    #endregion

    #region 缓存的函数指针

    private static RENDERDOC_TriggerCaptureDelegate? _triggerCapture;
    private static RENDERDOC_TriggerMultiFrameCaptureDelegate? _triggerMultiFrameCapture;
    private static RENDERDOC_StartFrameCaptureDelegate? _startFrameCapture;
    private static RENDERDOC_EndFrameCaptureDelegate? _endFrameCapture;
    private static RENDERDOC_SetCaptureFilePathTemplateDelegate? _setCaptureFilePathTemplate;
    private static RENDERDOC_GetNumCapturesDelegate? _getNumCaptures;
    private static RENDERDOC_GetCaptureDelegate? _getCapture;
    private static RENDERDOC_GetOverlayBitsDelegate? _getOverlayBits;
    private static RENDERDOC_MaskOverlayBitsDelegate? _maskOverlayBits;
    private static RENDERDOC_ShutdownDelegate? _shutdown;
    private static RENDERDOC_IsTargetControlConnectedDelegate? _isTargetControlConnected;
    private static RENDERDOC_LaunchReplayUIDelegate? _launchReplayUI;
    private static RENDERDOC_SetActiveWindowDelegate? _setActiveWindow;

    #endregion

    #region 状态

    private static bool _initialized;
    private static bool _available;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化 RenderDoc API，尝试加载 renderdoc.dll 并获取函数指针
    /// </summary>
    /// <returns>是否成功初始化</returns>
    public static bool Initialize()
    {
        if (_initialized)
        {
            return _available;
        }

        _initialized = true;

        var moduleHandle = GetModuleHandle(RenderDocDllName);

        if (moduleHandle == nint.Zero)
        {
            return false;
        }

        var getApiPtr = GetProcAddress(moduleHandle, "RENDERDOC_GetAPI");

        if (getApiPtr == nint.Zero)
        {
            return false;
        }

        var getApi = Marshal.GetDelegateForFunctionPointer<RENDERDOC_GetAPIDelegate>(getApiPtr);

        var result = getApi(RenderDocApiVersion, out var apiPointers);

        if (result != 1 || apiPointers == nint.Zero)
        {
            return false;
        }

        if (!LoadApiPointers(apiPointers))
        {
            return false;
        }

        _available = true;
        return true;
    }

    private static bool LoadApiPointers(nint apiPointers)
    {
        try
        {
            var offset = 0;

            _triggerCapture = LoadFunction<RENDERDOC_TriggerCaptureDelegate>(apiPointers, ref offset);
            _triggerMultiFrameCapture = LoadFunction<RENDERDOC_TriggerMultiFrameCaptureDelegate>(apiPointers, ref offset);
            _startFrameCapture = LoadFunction<RENDERDOC_StartFrameCaptureDelegate>(apiPointers, ref offset);
            _endFrameCapture = LoadFunction<RENDERDOC_EndFrameCaptureDelegate>(apiPointers, ref offset);
            _setCaptureFilePathTemplate = LoadFunction<RENDERDOC_SetCaptureFilePathTemplateDelegate>(apiPointers, ref offset);
            _getNumCaptures = LoadFunction<RENDERDOC_GetNumCapturesDelegate>(apiPointers, ref offset);
            _getCapture = LoadFunction<RENDERDOC_GetCaptureDelegate>(apiPointers, ref offset);
            offset += 3;

            _getOverlayBits = LoadFunction<RENDERDOC_GetOverlayBitsDelegate>(apiPointers, ref offset);
            _maskOverlayBits = LoadFunction<RENDERDOC_MaskOverlayBitsDelegate>(apiPointers, ref offset);
            offset += 2;

            _shutdown = LoadFunction<RENDERDOC_ShutdownDelegate>(apiPointers, ref offset);
            offset += 1;

            _isTargetControlConnected = LoadFunction<RENDERDOC_IsTargetControlConnectedDelegate>(apiPointers, ref offset);
            _launchReplayUI = LoadFunction<RENDERDOC_LaunchReplayUIDelegate>(apiPointers, ref offset);
            _setActiveWindow = LoadFunction<RENDERDOC_SetActiveWindowDelegate>(apiPointers, ref offset);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static T LoadFunction<T>(nint apiPointers, ref int offset) where T : Delegate
    {
        var funcPtr = Marshal.ReadIntPtr(apiPointers, offset * nint.Size);
        offset++;

        if (funcPtr == nint.Zero)
        {
            throw new InvalidOperationException($"RenderDoc API 函数指针为空：索引 {offset - 1}");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
    }

    #endregion

    #region 公开 API

    public static bool IsAvailable => _available;

    public static void TriggerCapture()
    {
        if (!_available || _triggerCapture == null)
        {
            return;
        }

        _triggerCapture();
    }

    public static void TriggerMultiFrameCapture(int numFrames)
    {
        if (!_available || _triggerMultiFrameCapture == null)
        {
            return;
        }

        _triggerMultiFrameCapture(numFrames);
    }

    public static void StartFrameCapture(nint device = 0, nint wndHandle = 0)
    {
        if (!_available || _startFrameCapture == null)
        {
            return;
        }

        _startFrameCapture(device, wndHandle);
    }

    public static bool EndFrameCapture(nint device = 0, nint wndHandle = 0)
    {
        if (!_available || _endFrameCapture == null)
        {
            return false;
        }

        return _endFrameCapture(device, wndHandle) != 0;
    }

    public static void SetCaptureFilePathTemplate(string filePath)
    {
        if (!_available || _setCaptureFilePathTemplate == null)
        {
            return;
        }

        _setCaptureFilePathTemplate(filePath);
    }

    public static int GetNumCaptures()
    {
        if (!_available || _getNumCaptures == null)
        {
            return 0;
        }

        return _getNumCaptures();
    }

    public static uint GetOverlayBits()
    {
        if (!_available || _getOverlayBits == null)
        {
            return 0;
        }

        return _getOverlayBits();
    }

    public static void MaskOverlayBits(uint andMask, uint orMask)
    {
        if (!_available || _maskOverlayBits == null)
        {
            return;
        }

        _maskOverlayBits(andMask, orMask);
    }

    public static void Shutdown()
    {
        if (!_available || _shutdown == null)
        {
            return;
        }

        _shutdown();
    }

    public static bool IsTargetControlConnected()
    {
        if (!_available || _isTargetControlConnected == null)
        {
            return false;
        }

        return _isTargetControlConnected() != 0;
    }

    public static uint LaunchReplayUI(int connectTargetControl = 1, string? cmdLine = null)
    {
        if (!_available || _launchReplayUI == null)
        {
            return 0;
        }

        return _launchReplayUI(connectTargetControl, cmdLine ?? "");
    }

    public static void SetActiveWindow(nint device, nint wndHandle)
    {
        if (!_available || _setActiveWindow == null)
        {
            return;
        }

        _setActiveWindow(device, wndHandle);
    }

    #endregion

    #region Overlay 常量

    public const uint Overlay_None = 0x00000000;
    public const uint Overlay_Enabled = 0x00000001;
    public const uint Overlay_FrameRate = 0x00000002;
    public const uint Overlay_FrameNumber = 0x00000004;
    public const uint Overlay_CaptureList = 0x00000008;
    public const uint Overlay_Default = Overlay_Enabled | Overlay_FrameRate | Overlay_FrameNumber | Overlay_CaptureList;
    public const uint Overlay_All = 0xFFFFFFFF;

    #endregion
}
