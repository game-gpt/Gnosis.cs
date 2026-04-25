using System.Text;
using GnosisInputDevice = Gnosis.Input.Device;
using SilkGL = Silk.NET.OpenGL;
using Sdl = Silk.NET.SDL.Sdl;
using Silk.NET.SDL;

namespace Gnosis.Graphic.Window;

public sealed unsafe class SdlGameWindow : IWindow
{
    #region 常量

    private const uint SdlInitVideo = 0x00000020;
    private const uint SdlWindowOpengl = 0x00000002;
    private const uint SdlWindowShown = 0x00000004;
    private const uint SdlWindowResizable = 0x00000020;
    private const uint SdlWindowFullscreen = 0x00000001;
    private const int SdlWindowposCentered = 0x2FFF0000;
    private const int SdlGlContextProfileCore = 1;

    #endregion

    #region 字段

    private readonly Sdl _sdl;
    private readonly Silk.NET.SDL.Window* _window;
    private readonly void* _glContext;
    private SilkGL.GL? _gl;
    private bool _isDisposed;
    private bool _isClosing;
    private bool _isMinimized;
    private int _width;
    private int _height;
    private string _title;
    private GnosisInputDevice.Keyboard? _gnosisKeyboard;
    private GnosisInputDevice.Mouse? _gnosisMouse;

    #endregion

    #region 属性

    public nint WindowHandle => (nint)_window;

    public uint Width => (uint)_width;

    public uint Height => (uint)_height;

    public bool IsClosing => _isClosing;

    public bool IsMinimized => _isMinimized;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            _sdl.SetWindowTitle(_window, value);
        }
    }

    public object? GlContext => _gl;

    #endregion

    #region 事件

    public event Action<EventArgs>? OnResize;
    public event Action<EventArgs>? OnClosing;

    #endregion

    #region 构造函数

    private SdlGameWindow(
        Sdl sdl,
        Silk.NET.SDL.Window* window,
        void* glContext,
        SilkGL.GL gl,
        string title,
        int width,
        int height)
    {
        _sdl = sdl;
        _window = window;
        _glContext = glContext;
        _gl = gl;
        _title = title;
        _width = width;
        _height = height;
    }

    #endregion

    #region IWindow 实现

    public void PollEvents()
    {
        Event sdlEvent = default;
        while (_sdl.PollEvent(ref sdlEvent) != 0)
        {
            ProcessEvent(ref sdlEvent);
        }
    }

    public void MakeCurrent()
    {
        _sdl.GLMakeCurrent(_window, _glContext);
    }

    public void SwapBuffers()
    {
        _sdl.GLSwapWindow(_window);
    }

    public static IWindow Create(WindowOptions options)
    {
        var sdl = Sdl.GetApi();

        if (sdl.Init(SdlInitVideo) < 0)
        {
            throw new InvalidOperationException($"SDL 初始化失败: {sdl.GetErrorS()}");
        }

        sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
        sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
        sdl.GLSetAttribute(GLattr.ContextProfileMask, SdlGlContextProfileCore);
        sdl.GLSetAttribute(GLattr.Doublebuffer, 1);
        sdl.GLSetAttribute(GLattr.DepthSize, 24);

        var windowFlags = SdlWindowOpengl | SdlWindowShown;
        if (options.Resizable) windowFlags |= SdlWindowResizable;
        if (options.Fullscreen) windowFlags |= SdlWindowFullscreen;

        var window = sdl.CreateWindow(
            options.Title,
            SdlWindowposCentered,
            SdlWindowposCentered,
            (int)options.Width,
            (int)options.Height,
            windowFlags
        );

        if (window == null)
        {
            throw new InvalidOperationException($"SDL 窗口创建失败: {sdl.GetErrorS()}");
        }

        var glContext = sdl.GLCreateContext(window);
        if (glContext == null)
        {
            throw new InvalidOperationException($"OpenGL 上下文创建失败: {sdl.GetErrorS()}");
        }

        sdl.GLMakeCurrent(window, glContext);

        var gl = SilkGL.GL.GetApi(CreateProcAddressLoader(sdl));

        if (options.VSync)
        {
            sdl.GLSetSwapInterval(1);
        }

        Console.WriteLine($"[SdlGameWindow] 窗口创建成功 - {options.Width}x{options.Height} - OpenGL 3.3 Core");

        return new SdlGameWindow(sdl, window, glContext, gl, options.Title, (int)options.Width, (int)options.Height);
    }

    #endregion

    #region GL 函数指针加载

    public nint GetGLProcAddress(string name)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(name + "\0");
        fixed (byte* ptr = bytes)
        {
            return (nint)_sdl.GLGetProcAddress(ptr);
        }
    }

    private static Func<string, nint> CreateProcAddressLoader(Sdl sdl)
    {
        return name =>
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name + "\0");
            fixed (byte* ptr = bytes)
            {
                return (nint)sdl.GLGetProcAddress(ptr);
            }
        };
    }

    #endregion

    #region 输入桥接

    public void SetInputDevices(GnosisInputDevice.Keyboard keyboard, GnosisInputDevice.Mouse mouse)
    {
        _gnosisKeyboard = keyboard;
        _gnosisMouse = mouse;
    }

    #endregion

    #region 事件处理

    private void ProcessEvent(ref Event sdlEvent)
    {
        switch ((EventType)sdlEvent.Type)
        {
            case EventType.Quit:
                _isClosing = true;
                OnClosing?.Invoke(EventArgs.Empty);
                break;

            case EventType.Windowevent:
                HandleWindowEvent(ref sdlEvent.Window);
                break;

            case EventType.Keydown:
                HandleKeyDown(ref sdlEvent.Key);
                break;

            case EventType.Keyup:
                HandleKeyUp(ref sdlEvent.Key);
                break;

            case EventType.Mousebuttondown:
                HandleMouseDown(ref sdlEvent.Button);
                break;

            case EventType.Mousebuttonup:
                HandleMouseUp(ref sdlEvent.Button);
                break;

            case EventType.Mousemotion:
                HandleMouseMove(ref sdlEvent.Motion);
                break;

            case EventType.Mousewheel:
                HandleMouseWheel(ref sdlEvent.Wheel);
                break;
        }
    }

    private void HandleWindowEvent(ref WindowEvent windowEvent)
    {
        switch ((WindowEventID)windowEvent.Event)
        {
            case WindowEventID.Close:
                _isClosing = true;
                OnClosing?.Invoke(EventArgs.Empty);
                break;

            case WindowEventID.SizeChanged:
            case WindowEventID.Resized:
                _width = windowEvent.Data1;
                _height = windowEvent.Data2;
                _isMinimized = _width <= 0 || _height <= 0;
                OnResize?.Invoke(EventArgs.Empty);
                break;

            case WindowEventID.Minimized:
                _isMinimized = true;
                break;

            case WindowEventID.Restored:
                _isMinimized = false;
                break;
        }
    }

    private void HandleKeyDown(ref KeyboardEvent keyEvent)
    {
        var keyCode = MapKeyCode(keyEvent.Keysym.Sym);
        _gnosisKeyboard?.SetKeyState(keyCode, true);
    }

    private void HandleKeyUp(ref KeyboardEvent keyEvent)
    {
        var keyCode = MapKeyCode(keyEvent.Keysym.Sym);
        _gnosisKeyboard?.SetKeyState(keyCode, false);
    }

    private void HandleMouseDown(ref MouseButtonEvent buttonEvent)
    {
        var button = MapMouseButton(buttonEvent.Button);
        _gnosisMouse?.SetButtonState(button, true);
    }

    private void HandleMouseUp(ref MouseButtonEvent buttonEvent)
    {
        var button = MapMouseButton(buttonEvent.Button);
        _gnosisMouse?.SetButtonState(button, false);
    }

    private void HandleMouseMove(ref MouseMotionEvent motionEvent)
    {
        _gnosisMouse?.SetPosition(motionEvent.X, motionEvent.Y);
    }

    private void HandleMouseWheel(ref MouseWheelEvent wheelEvent)
    {
        _gnosisMouse?.SetScrollDelta(wheelEvent.Y);
    }

    #endregion

    #region 键码映射

    private static int MapKeyCode(int sdlKey)
    {
        return sdlKey switch
        {
            (int)KeyCode.KA => GnosisInputDevice.KeyCode.A,
            (int)KeyCode.KB => GnosisInputDevice.KeyCode.B,
            (int)KeyCode.KC => GnosisInputDevice.KeyCode.C,
            (int)KeyCode.KD => GnosisInputDevice.KeyCode.D,
            (int)KeyCode.KE => GnosisInputDevice.KeyCode.E,
            (int)KeyCode.KF => GnosisInputDevice.KeyCode.F,
            (int)KeyCode.KG => GnosisInputDevice.KeyCode.G,
            (int)KeyCode.KH => GnosisInputDevice.KeyCode.H,
            (int)KeyCode.KI => GnosisInputDevice.KeyCode.I,
            (int)KeyCode.KJ => GnosisInputDevice.KeyCode.J,
            (int)KeyCode.KK => GnosisInputDevice.KeyCode.K,
            (int)KeyCode.KL => GnosisInputDevice.KeyCode.L,
            (int)KeyCode.KM => GnosisInputDevice.KeyCode.M,
            (int)KeyCode.KN => GnosisInputDevice.KeyCode.N,
            (int)KeyCode.KO => GnosisInputDevice.KeyCode.O,
            (int)KeyCode.KP => GnosisInputDevice.KeyCode.P,
            (int)KeyCode.KQ => GnosisInputDevice.KeyCode.Q,
            (int)KeyCode.KR => GnosisInputDevice.KeyCode.R,
            (int)KeyCode.KS => GnosisInputDevice.KeyCode.S,
            (int)KeyCode.KT => GnosisInputDevice.KeyCode.T,
            (int)KeyCode.KU => GnosisInputDevice.KeyCode.U,
            (int)KeyCode.KV => GnosisInputDevice.KeyCode.V,
            (int)KeyCode.KW => GnosisInputDevice.KeyCode.W,
            (int)KeyCode.KX => GnosisInputDevice.KeyCode.X,
            (int)KeyCode.KY => GnosisInputDevice.KeyCode.Y,
            (int)KeyCode.KZ => GnosisInputDevice.KeyCode.Z,
            (int)KeyCode.KLeft => GnosisInputDevice.KeyCode.Left,
            (int)KeyCode.KRight => GnosisInputDevice.KeyCode.Right,
            (int)KeyCode.KUp => GnosisInputDevice.KeyCode.Up,
            (int)KeyCode.KDown => GnosisInputDevice.KeyCode.Down,
            (int)KeyCode.KSpace => GnosisInputDevice.KeyCode.Space,
            (int)KeyCode.KEscape => GnosisInputDevice.KeyCode.Escape,
            (int)KeyCode.KReturn => GnosisInputDevice.KeyCode.Enter,
            (int)KeyCode.KLshift => GnosisInputDevice.KeyCode.LeftShift,
            (int)KeyCode.KRshift => GnosisInputDevice.KeyCode.RightShift,
            (int)KeyCode.KLctrl => GnosisInputDevice.KeyCode.LeftControl,
            (int)KeyCode.KRctrl => GnosisInputDevice.KeyCode.RightControl,
            (int)KeyCode.KF1 => GnosisInputDevice.KeyCode.F1,
            (int)KeyCode.KF2 => GnosisInputDevice.KeyCode.F2,
            (int)KeyCode.KF3 => GnosisInputDevice.KeyCode.F3,
            (int)KeyCode.KF4 => GnosisInputDevice.KeyCode.F4,
            (int)KeyCode.KF5 => GnosisInputDevice.KeyCode.F5,
            (int)KeyCode.KF6 => GnosisInputDevice.KeyCode.F6,
            (int)KeyCode.KF7 => GnosisInputDevice.KeyCode.F7,
            (int)KeyCode.KF8 => GnosisInputDevice.KeyCode.F8,
            (int)KeyCode.KF9 => GnosisInputDevice.KeyCode.F9,
            (int)KeyCode.KF10 => GnosisInputDevice.KeyCode.F10,
            (int)KeyCode.KF11 => GnosisInputDevice.KeyCode.F11,
            (int)KeyCode.KF12 => GnosisInputDevice.KeyCode.F12,
            (int)KeyCode.K0 => GnosisInputDevice.KeyCode.D0,
            (int)KeyCode.K1 => GnosisInputDevice.KeyCode.D1,
            (int)KeyCode.K2 => GnosisInputDevice.KeyCode.D2,
            (int)KeyCode.K3 => GnosisInputDevice.KeyCode.D3,
            (int)KeyCode.K4 => GnosisInputDevice.KeyCode.D4,
            (int)KeyCode.K5 => GnosisInputDevice.KeyCode.D5,
            (int)KeyCode.K6 => GnosisInputDevice.KeyCode.D6,
            (int)KeyCode.K7 => GnosisInputDevice.KeyCode.D7,
            (int)KeyCode.K8 => GnosisInputDevice.KeyCode.D8,
            (int)KeyCode.K9 => GnosisInputDevice.KeyCode.D9,
            _ => -1
        };
    }

    private static int MapMouseButton(byte button)
    {
        return button switch
        {
            1 => GnosisInputDevice.MouseButton.Left,
            2 => GnosisInputDevice.MouseButton.Middle,
            3 => GnosisInputDevice.MouseButton.Right,
            4 => GnosisInputDevice.MouseButton.Back,
            5 => GnosisInputDevice.MouseButton.Forward,
            _ => -1
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_gl is not null)
        {
            _gl.Dispose();
            _gl = null;
        }

        if (_glContext != null)
        {
            _sdl.GLDeleteContext(_glContext);
        }

        if (_window != null)
        {
            _sdl.DestroyWindow(_window);
        }

        _sdl.Quit();
        _isDisposed = true;
    }

    #endregion
}
