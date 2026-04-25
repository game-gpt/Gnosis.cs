using System.Runtime.InteropServices;
using System.Text;
using Gnosis.Input.Device;
using Gnosis.Platform.GL;

namespace Gnosis.Platform.Win32;

public sealed unsafe class Win32Window : IPlatformWindow
{
    #region 字段

    private nint _hWnd;
    private nint _hDc;
    private nint _hGlrc;
    private GLContext? _gl;
    private bool _isDisposed;
    private bool _isClosing;
    private bool _isMinimized;
    private int _width;
    private int _height;
    private string _title;
    private Keyboard? _keyboard;
    private Mouse? _mouse;
    private NativeMethods.WndProc? _wndProc;
    private NativeMethods.WglCreateContextAttribsArbProc? _wglCreateContextAttribsArb;

    #endregion

    #region 属性

    public nint Handle => _hWnd;
    public uint Width => (uint)_width;
    public uint Height => (uint)_height;
    public bool IsClosing => _isClosing;
    public bool IsMinimized => _isMinimized;
    public GLContext? GL => _gl;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_hWnd != nint.Zero)
            {
                NativeMethods.SetWindowTextW(_hWnd, value);
            }
        }
    }

    #endregion

    #region 事件

    public event Action<uint, uint>? OnResize;
    public event Action? OnClosing;

    #endregion

    #region 构造函数

    private Win32Window(nint hWnd, nint hDc, nint hGlrc, GLContext gl, string title, int width, int height)
    {
        _hWnd = hWnd;
        _hDc = hDc;
        _hGlrc = hGlrc;
        _gl = gl;
        _title = title;
        _width = width;
        _height = height;
    }

    #endregion

    #region IPlatformWindow 实现

    public void PollEvents()
    {
        while (NativeMethods.PeekMessageW(out var msg, nint.Zero, 0, 0, NativeMethods.PmRemove))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessageW(ref msg);
        }
    }

    public void MakeCurrent()
    {
        if (_hDc != nint.Zero && _hGlrc != nint.Zero)
        {
            NativeMethods.wglMakeCurrent(_hDc, _hGlrc);
        }
    }

    public void SwapBuffers()
    {
        if (_hDc != nint.Zero)
        {
            NativeMethods.SwapBuffers(_hDc);
        }
    }

    public nint GetProcAddress(string name)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(name + "\0");
        fixed (byte* ptr = bytes)
        {
            var proc = NativeMethods.wglGetProcAddress(ptr);
            if (proc != nint.Zero)
            {
                return proc;
            }

            proc = GetOpengl32ProcAddress(name);
            if (proc == nint.Zero)
            {
                Console.WriteLine($"[Win32Window] 警告: 无法加载 GL 函数 '{name}' (wglGetProcAddress 和 GetProcAddress 均返回 null)");
            }

            return proc;
        }
    }

    public static Win32Window Create(WindowCreateInfo info)
    {
        var hInstance = NativeMethods.GetModuleHandleW(nint.Zero);

        var className = "GnosisWindowClass_" + Guid.NewGuid().ToString("N");
        var classNamePtr = NativeMethods.ToPtr(className);

        var wndProc = new NativeMethods.WndProc(StaticWndProc);
        GCHandle wndProcHandle = GCHandle.Alloc(wndProc);

        var wc = new NativeMethods.WndClassExW
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.WndClassExW>(),
            Style = NativeMethods.CsOwndc | NativeMethods.CsHredraw | NativeMethods.CsVredraw,
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            HInstance = hInstance,
            HCursor = NativeMethods.LoadCursorW(nint.Zero, (nint)32512),
            LpszClassName = classNamePtr
        };

        ushort regResult = NativeMethods.RegisterClassExW(ref wc);
        if (regResult == 0)
        {
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException($"窗口类注册失败: {Marshal.GetLastWin32Error()}");
        }

        var style = NativeMethods.WsOverlappedWindow | NativeMethods.WsClipChildren | NativeMethods.WsClipsiblings;
        if (info.Resizable)
        {
            style |= NativeMethods.WsThickFrame;
        }
        else
        {
            style &= ~(NativeMethods.WsThickFrame | NativeMethods.WsMaximizebox);
        }

        var exStyle = 0u;
        int x = NativeMethods.CwUsedefault;
        int y = NativeMethods.CwUsedefault;
        int width = (int)info.Width;
        int height = (int)info.Height;

        var titlePtr = NativeMethods.ToPtr(info.Title);
        var hWnd = NativeMethods.CreateWindowExW(
            exStyle, classNamePtr, titlePtr,
            style, x, y, width, height,
            nint.Zero, nint.Zero, hInstance, nint.Zero);

        NativeMethods.FreePtr(titlePtr);

        if (hWnd == nint.Zero)
        {
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException($"窗口创建失败: {Marshal.GetLastWin32Error()}");
        }

        var hDc = NativeMethods.GetDC(hWnd);
        if (hDc == nint.Zero)
        {
            NativeMethods.DestroyWindow(hWnd);
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException("获取设备上下文失败");
        }

        var pfd = new NativeMethods.PixelFormatDescriptor
        {
            NSize = (ushort)Marshal.SizeOf<NativeMethods.PixelFormatDescriptor>(),
            NVersion = 1,
            DwFlags = NativeMethods.PfdDrawToWindow | NativeMethods.PfdSupportOpengl | NativeMethods.PfdDoublebuffer,
            IPixelType = NativeMethods.PfdTypeRgba,
            CColorBits = 32,
            CDepthBits = 24,
            CStencilBits = 8,
            ILayerType = (byte)NativeMethods.PfdMainPlane
        };

        int pixelFormat = NativeMethods.ChoosePixelFormat(hDc, ref pfd);
        if (pixelFormat == 0)
        {
            NativeMethods.ReleaseDC(hWnd, hDc);
            NativeMethods.DestroyWindow(hWnd);
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException("像素格式选择失败");
        }

        if (!NativeMethods.SetPixelFormat(hDc, pixelFormat, ref pfd))
        {
            NativeMethods.ReleaseDC(hWnd, hDc);
            NativeMethods.DestroyWindow(hWnd);
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException("像素格式设置失败");
        }

        var tempContext = NativeMethods.wglCreateContext(hDc);
        if (tempContext == nint.Zero)
        {
            NativeMethods.ReleaseDC(hWnd, hDc);
            NativeMethods.DestroyWindow(hWnd);
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException("临时 OpenGL 上下文创建失败");
        }

        NativeMethods.wglMakeCurrent(hDc, tempContext);

        var wglCreateContextAttribsArbPtr = NativeMethods.wglGetProcAddress(
            (byte*)Marshal.StringToHGlobalAnsi("wglCreateContextAttribsARB"));
        var wglCreateContextAttribsArb = default(NativeMethods.WglCreateContextAttribsArbProc?);

        nint hGlrc;

        if (wglCreateContextAttribsArbPtr != nint.Zero)
        {
            wglCreateContextAttribsArb = Marshal.GetDelegateForFunctionPointer<NativeMethods.WglCreateContextAttribsArbProc>(wglCreateContextAttribsArbPtr);

            int[] attribs =
            [
                NativeMethods.WglContextMajorVersionArb, 3,
                NativeMethods.WglContextMinorVersionArb, 3,
                NativeMethods.WglContextProfileMaskArb, NativeMethods.WglContextCoreProfileBitArb,
                0
            ];

            fixed (int* pAttribs = attribs)
            {
                hGlrc = wglCreateContextAttribsArb(hDc, nint.Zero, pAttribs);
            }

            if (hGlrc == nint.Zero)
            {
                int[] fallbackAttribs =
                [
                    NativeMethods.WglContextMajorVersionArb, 2,
                    NativeMethods.WglContextMinorVersionArb, 1,
                    0
                ];

                fixed (int* pAttribs = fallbackAttribs)
                {
                    hGlrc = wglCreateContextAttribsArb(hDc, nint.Zero, pAttribs);
                }
            }
        }
        else
        {
            hGlrc = NativeMethods.wglCreateContext(hDc);
        }

        NativeMethods.wglMakeCurrent(nint.Zero, nint.Zero);
        NativeMethods.wglDeleteContext(tempContext);

        if (hGlrc == nint.Zero)
        {
            NativeMethods.ReleaseDC(hWnd, hDc);
            NativeMethods.DestroyWindow(hWnd);
            NativeMethods.FreePtr(classNamePtr);
            wndProcHandle.Free();
            throw new InvalidOperationException("OpenGL 3.3 上下文创建失败");
        }

        NativeMethods.wglMakeCurrent(hDc, hGlrc);

        var gl = new GLContext(name =>
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name + "\0");
            fixed (byte* ptr = bytes)
            {
                var proc = NativeMethods.wglGetProcAddress(ptr);
                if (proc == nint.Zero)
                {
                    proc = GetOpengl32ProcAddress(name);
                }

                return proc;
            }
        });

        NativeMethods.GetClientRect(hWnd, out var rect);
        int clientWidth = rect.Right - rect.Left;
        int clientHeight = rect.Bottom - rect.Top;

        var window = new Win32Window(hWnd, hDc, hGlrc, gl, info.Title, clientWidth, clientHeight)
        {
            _wndProc = wndProc,
            _wglCreateContextAttribsArb = wglCreateContextAttribsArb
        };

        var windowHandle = GCHandle.Alloc(window);
        NativeMethods.SetWindowLongPtrW(hWnd, NativeMethods.GWLP_USERDATA, GCHandle.ToIntPtr(windowHandle));

        if (info.Visible)
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SwShow);
        }

        NativeMethods.FreePtr(classNamePtr);

        Console.WriteLine($"[Win32Window] 窗口创建成功 - {clientWidth}x{clientHeight} - OpenGL 3.3 Core (自研平台层)");

        return window;
    }

    #endregion

    #region 输入桥接

    public void SetInputDevices(Keyboard keyboard, Mouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }

    #endregion

    #region 窗口过程

    private static nint StaticWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        var userData = NativeMethods.GetWindowLongPtrW(hWnd, NativeMethods.GWLP_USERDATA);
        if (userData != nint.Zero)
        {
            var handle = GCHandle.FromIntPtr(userData);
            if (handle.IsAllocated && handle.Target is Win32Window window)
            {
                return window.WndProc(hWnd, msg, wParam, lParam);
            }
        }

        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case NativeMethods.WmClose:
                _isClosing = true;
                OnClosing?.Invoke();
                return nint.Zero;

            case NativeMethods.WmDestroy:
                NativeMethods.PostQuitMessage(0);
                return nint.Zero;

            case NativeMethods.WmSize:
                var sizeType = (int)(nint)wParam;
                var newWidth = (int)(lParam & 0xFFFF);
                var newHeight = (int)((lParam >> 16) & 0xFFFF);

                if (sizeType == NativeMethods.SizeMinimized)
                {
                    _isMinimized = true;
                }
                else if (sizeType == NativeMethods.SizeRestored)
                {
                    _isMinimized = false;
                }

                if (newWidth > 0 && newHeight > 0)
                {
                    _width = newWidth;
                    _height = newHeight;
                    OnResize?.Invoke((uint)newWidth, (uint)newHeight);
                }
                break;

            case NativeMethods.WmKeydown:
            case NativeMethods.WmSyskeydown:
                HandleKeyDown((int)wParam);
                break;

            case NativeMethods.WmKeyup:
            case NativeMethods.WmSyskeyup:
                HandleKeyUp((int)wParam);
                break;

            case NativeMethods.WmMousemove:
                var mouseX = (short)(lParam & 0xFFFF);
                var mouseY = (short)((lParam >> 16) & 0xFFFF);
                _mouse?.SetPosition(mouseX, mouseY);
                break;

            case NativeMethods.WmLbuttondown:
                _mouse?.SetButtonState(MouseButton.Left, true);
                break;

            case NativeMethods.WmLbuttonup:
                _mouse?.SetButtonState(MouseButton.Left, false);
                break;

            case NativeMethods.WmRbuttondown:
                _mouse?.SetButtonState(MouseButton.Right, true);
                break;

            case NativeMethods.WmRbuttonup:
                _mouse?.SetButtonState(MouseButton.Right, false);
                break;

            case NativeMethods.WmMbuttondown:
                _mouse?.SetButtonState(MouseButton.Middle, true);
                break;

            case NativeMethods.WmMbuttonup:
                _mouse?.SetButtonState(MouseButton.Middle, false);
                break;

            case NativeMethods.WmMousewheel:
                var delta = (short)((wParam >> 16) & 0xFFFF);
                _mouse?.SetScrollDelta(delta / 120f);
                break;
        }

        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    #endregion

    #region 键码映射

    private void HandleKeyDown(int vk)
    {
        var keyCode = MapVirtualKey(vk);
        if (keyCode >= 0)
        {
            _keyboard?.SetKeyState(keyCode, true);
        }
    }

    private void HandleKeyUp(int vk)
    {
        var keyCode = MapVirtualKey(vk);
        if (keyCode >= 0)
        {
            _keyboard?.SetKeyState(keyCode, false);
        }
    }

    private static int MapVirtualKey(int vk)
    {
        return vk switch
        {
            0x41 => KeyCode.A, 0x42 => KeyCode.B, 0x43 => KeyCode.C, 0x44 => KeyCode.D,
            0x45 => KeyCode.E, 0x46 => KeyCode.F, 0x47 => KeyCode.G, 0x48 => KeyCode.H,
            0x49 => KeyCode.I, 0x4A => KeyCode.J, 0x4B => KeyCode.K, 0x4C => KeyCode.L,
            0x4D => KeyCode.M, 0x4E => KeyCode.N, 0x4F => KeyCode.O, 0x50 => KeyCode.P,
            0x51 => KeyCode.Q, 0x52 => KeyCode.R, 0x53 => KeyCode.S, 0x54 => KeyCode.T,
            0x55 => KeyCode.U, 0x56 => KeyCode.V, 0x57 => KeyCode.W, 0x58 => KeyCode.X,
            0x59 => KeyCode.Y, 0x5A => KeyCode.Z,
            0x30 => KeyCode.D0, 0x31 => KeyCode.D1, 0x32 => KeyCode.D2, 0x33 => KeyCode.D3,
            0x34 => KeyCode.D4, 0x35 => KeyCode.D5, 0x36 => KeyCode.D6, 0x37 => KeyCode.D7,
            0x38 => KeyCode.D8, 0x39 => KeyCode.D9,
            0x25 => KeyCode.Left, 0x26 => KeyCode.Up, 0x27 => KeyCode.Right, 0x28 => KeyCode.Down,
            0x20 => KeyCode.Space, 0x1B => KeyCode.Escape, 0x0D => KeyCode.Enter,
            0x08 => KeyCode.Backspace, 0x09 => KeyCode.Tab,
            0x10 => KeyCode.LeftShift, 0xA0 => KeyCode.LeftShift, 0xA1 => KeyCode.RightShift,
            0x11 => KeyCode.LeftControl, 0xA2 => KeyCode.LeftControl, 0xA3 => KeyCode.RightControl,
            0x12 => KeyCode.LeftAlt, 0xA4 => KeyCode.LeftAlt, 0xA5 => KeyCode.RightAlt,
            0x70 => KeyCode.F1, 0x71 => KeyCode.F2, 0x72 => KeyCode.F3, 0x73 => KeyCode.F4,
            0x74 => KeyCode.F5, 0x75 => KeyCode.F6, 0x76 => KeyCode.F7, 0x77 => KeyCode.F8,
            0x78 => KeyCode.F9, 0x79 => KeyCode.F10, 0x7A => KeyCode.F11, 0x7B => KeyCode.F12,
            0x14 => KeyCode.CapsLock, 0x90 => KeyCode.NumLock,
            _ => -1
        };
    }

    #endregion

    #region 辅助

    private static nint GetOpengl32ProcAddress(string name)
    {
        if (NativeLibrary.TryLoad("opengl32.dll", out var hModule) && hModule != nint.Zero)
        {
            if (NativeLibrary.TryGetExport(hModule, name, out var proc))
            {
                return proc;
            }
        }

        return nint.Zero;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_hGlrc != nint.Zero)
        {
            NativeMethods.wglMakeCurrent(nint.Zero, nint.Zero);
            NativeMethods.wglDeleteContext(_hGlrc);
            _hGlrc = nint.Zero;
        }

        if (_hDc != nint.Zero && _hWnd != nint.Zero)
        {
            NativeMethods.ReleaseDC(_hWnd, _hDc);
            _hDc = nint.Zero;
        }

        if (_hWnd != nint.Zero)
        {
            var userData = NativeMethods.GetWindowLongPtrW(_hWnd, NativeMethods.GWLP_USERDATA);
            if (userData != nint.Zero)
            {
                var handle = GCHandle.FromIntPtr(userData);
                handle.Free();
            }

            NativeMethods.DestroyWindow(_hWnd);
            _hWnd = nint.Zero;
        }

        _gl?.Dispose();
        _gl = null;
        _isDisposed = true;
    }

    #endregion
}
