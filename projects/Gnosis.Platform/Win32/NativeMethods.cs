using System.Runtime.InteropServices;
using System.Text;

namespace Gnosis.Platform.Win32;

internal static unsafe class NativeMethods
{
    #region 常量

    public const nint HInstance = 0;
    public const int SwShow = 5;
    public const uint WsOverlappedWindow = 0x00CF0000;
    public const uint WsVisible = 0x10000000;
    public const uint WsThickFrame = 0x00040000;
    public const uint WsCaption = 0x00C00000;
    public const uint WsSysMenu = 0x00080000;
    public const uint WsMinimizebox = 0x00020000;
    public const uint WsMaximizebox = 0x00010000;
    public const uint WsClipChildren = 0x02000000;
    public const uint WsClipsiblings = 0x04000000;
    public const int CwUsedefault = -2147483648;
    public const uint WmDestroy = 0x0002;
    public const uint WmSize = 0x0005;
    public const uint WmKeydown = 0x0100;
    public const uint WmKeyup = 0x0101;
    public const uint WmChar = 0x0102;
    public const uint WmMousemove = 0x0200;
    public const uint WmLbuttondown = 0x0201;
    public const uint WmLbuttonup = 0x0202;
    public const uint WmRbuttondown = 0x0204;
    public const uint WmRbuttonup = 0x0205;
    public const uint WmMbuttondown = 0x0207;
    public const uint WmMbuttonup = 0x0208;
    public const uint WmMousewheel = 0x020A;
    public const uint WmClose = 0x0010;
    public const uint WmSyskeydown = 0x0104;
    public const uint WmSyskeyup = 0x0105;
    public const int SizeMinimized = 1;
    public const int SizeRestored = 0;
    public const uint PfdDrawToWindow = 0x00000004;
    public const uint PfdSupportOpengl = 0x00000020;
    public const uint PfdDoublebuffer = 0x00000001;
    public const byte PfdTypeRgba = 0;
    public const int PfdMainPlane = 0;
    public const uint CsOwndc = 0x0020;
    public const uint CsHredraw = 0x0002;
    public const uint CsVredraw = 0x0001;
    public const int WglContextMajorVersionArb = 0x2091;
    public const int WglContextMinorVersionArb = 0x2092;
    public const int WglContextProfileMaskArb = 0x9126;
    public const int WglContextCoreProfileBitArb = 0x00000001;
    public const int WglContextFlagsArb = 0x2094;
    public const int WglContextDebugBitArb = 0x0001;
    public const uint PmRemove = 0x0001;

    #endregion

    #region 结构体

    [StructLayout(LayoutKind.Sequential)]
    public struct WndClassExW
    {
        public uint CbSize;
        public uint Style;
        public nint LpfnWndProc;
        public int CbClsExtra;
        public int CbWndExtra;
        public nint HInstance;
        public nint HIcon;
        public nint HCursor;
        public nint HbrBackground;
        public nint LpszMenuName;
        public nint LpszClassName;
        public nint HIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormatDescriptor
    {
        public ushort NSize;
        public ushort NVersion;
        public uint DwFlags;
        public byte IPixelType;
        public byte CColorBits;
        public byte CRedBits;
        public byte CRedShift;
        public byte CGreenBits;
        public byte CGreenShift;
        public byte CBlueBits;
        public byte CBlueShift;
        public byte CAlphaBits;
        public byte CAlphaShift;
        public byte CAccumBits;
        public byte CAccumRedBits;
        public byte CAccumGreenBits;
        public byte CAccumBlueBits;
        public byte CAccumAlphaBits;
        public byte CDepthBits;
        public byte CStencilBits;
        public byte CAuxBuffers;
        public byte ILayerType;
        public byte BReserved;
        public uint DwLayerMask;
        public uint DwVisibleMask;
        public uint DwDamageMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Msg
    {
        public nint HWnd;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    #region 委托

    public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    public delegate nint WglCreateContextAttribsArbProc(nint hdc, nint hShareContext, int* attribList);

    #endregion

    #region User32

    [DllImport("user32.dll", SetLastError = true)]
    public static extern ushort RegisterClassExW(ref WndClassExW lpwcx);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint CreateWindowExW(
        uint dwExStyle, nint lpClassName, nint lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool PeekMessageW(out Msg lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    public static extern nint DispatchMessageW(ref Msg lpMsg);

    [DllImport("user32.dll")]
    public static extern nint DefWindowProcW(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(nint hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowTextW(nint hWnd, string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    public static extern nint LoadCursorW(nint hInstance, nint lpCursorName);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("kernel32.dll")]
    public static extern nint GetModuleHandleW(nint lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint LoadLibraryW(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetProcAddress(nint hModule, byte* lpProcName);

    [DllImport("user32.dll")]
    public static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    public static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

    public const int GWLP_WNDPROC = -4;
    public const int GWLP_USERDATA = -21;

    #endregion

    #region GDI32

    [DllImport("user32.dll")]
    public static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    public static extern int ChoosePixelFormat(nint hdc, ref PixelFormatDescriptor ppfd);

    [DllImport("gdi32.dll")]
    public static extern bool SetPixelFormat(nint hdc, int format, ref PixelFormatDescriptor ppfd);

    [DllImport("gdi32.dll")]
    public static extern bool SwapBuffers(nint hdc);

    #endregion

    #region OpenGL32

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern nint wglCreateContext(nint hdc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern int wglMakeCurrent(nint hdc, nint hglrc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern int wglDeleteContext(nint hglrc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern nint wglGetProcAddress(byte* name);

    [DllImport("opengl32.dll")]
    public static extern nint wglGetCurrentContext();

    #endregion

    #region 辅助方法

    public static nint ToPtr(string s)
    {
        return Marshal.StringToHGlobalUni(s);
    }

    public static void FreePtr(nint ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }

    #endregion
}
