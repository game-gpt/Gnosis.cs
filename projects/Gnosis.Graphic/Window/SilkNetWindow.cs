using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Graphic.RHI;
using GnosisInputDevice = Gnosis.Input.Device;
using SilkInput = Silk.NET.Input;
using SilkGL = Silk.NET.OpenGL;
using SilkWindowing = Silk.NET.Windowing;

using SilkInputWindowExtensions = Silk.NET.Input.InputWindowExtensions;

namespace Gnosis.Graphic.Window;

public sealed class SilkNetWindow : IWindow
{
    #region 字段

    private readonly SilkWindowing.IWindow _window;
    private SilkInput.IInputContext? _inputContext;
    private SilkInput.IKeyboard? _silkKeyboard;
    private SilkInput.IMouse? _silkMouse;
    private GnosisInputDevice.Keyboard? _gnosisKeyboard;
    private GnosisInputDevice.Mouse? _gnosisMouse;
    private bool _isDisposed;
    private bool _isInitialized;

    #endregion

    #region 属性

    public nint WindowHandle
    {
        get
        {
            try
            {
                return _window.Handle;
            }
            catch
            {
                return 0;
            }
        }
    }

    public uint Width => (uint)_window.Size.X;

    public uint Height => (uint)_window.Size.Y;

    public bool IsClosing => _window.IsClosing;

    public bool IsMinimized => _window.Size.X <= 0 || _window.Size.Y <= 0;

    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    public object? GlContext { get; private set; }

    #endregion

    #region 事件

    public event Action<EventArgs>? OnResize;
    public event Action<EventArgs>? OnClosing;

    #endregion

    #region 构造函数

    private SilkNetWindow(SilkWindowing.IWindow window)
    {
        _window = window;
        _window.Resize += OnWindowResize;
        _window.Closing += OnWindowClosing;
    }

    #endregion

    #region IWindow 实现

    public void PollEvents()
    {
        if (!_isInitialized)
        {
            _window.Initialize();
            InitializeInput();
            InitializeGL();
            _isInitialized = true;
        }

        _window.DoEvents();
    }

    public void MakeCurrent()
    {
        if (OperatingSystem.IsWindows())
        {
            var hglrc = wglGetCurrentContext();
            if (hglrc == 0)
            {
                return;
            }

            var hdc = wglGetCurrentDC();
            if (hdc == 0)
            {
                return;
            }

            wglMakeCurrent(hdc, hglrc);
        }
    }

    public void SwapBuffers()
    {
        if (OperatingSystem.IsWindows())
        {
            var hdc = wglGetCurrentDC();
            if (hdc != 0)
            {
                SwapBuffersNative(hdc);
            }
        }
    }

    public static IWindow Create(WindowOptions options)
    {
        var windowOptions = SilkWindowing.WindowOptions.Default;
        windowOptions.Title = options.Title;
        windowOptions.Size = new Silk.NET.Maths.Vector2D<int>((int)options.Width, (int)options.Height);
        windowOptions.VSync = options.VSync;
        windowOptions.WindowState = options.Fullscreen ? SilkWindowing.WindowState.Fullscreen : SilkWindowing.WindowState.Normal;
        windowOptions.WindowBorder = options.Resizable ? SilkWindowing.WindowBorder.Resizable : SilkWindowing.WindowBorder.Fixed;

        var silkWindow = SilkWindowing.Window.Create(windowOptions);

        return new SilkNetWindow(silkWindow);
    }

    #endregion

    #region 原生方法

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hWnd);

    [DllImport("opengl32.dll")]
    private static extern nint wglGetCurrentContext();

    [DllImport("opengl32.dll")]
    private static extern nint wglGetCurrentDC();

    [DllImport("opengl32.dll")]
    private static extern bool wglMakeCurrent(nint hdc, nint hglrc);

    [DllImport("gdi32.dll", EntryPoint = "SwapBuffers")]
    private static extern bool SwapBuffersNative(nint hdc);

    #endregion

    #region GL 初始化

    private void InitializeGL()
    {
        try
        {
            var gl = SilkGL.GL.GetApi(_window);
            GlContext = gl;
            Console.WriteLine("[SilkNetWindow] OpenGL 上下文创建成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SilkNetWindow] OpenGL 上下文创建失败: {ex.Message}");
        }
    }

    #endregion

    #region 输入桥接

    public void SetInputDevices(GnosisInputDevice.Keyboard keyboard, GnosisInputDevice.Mouse mouse)
    {
        _gnosisKeyboard = keyboard;
        _gnosisMouse = mouse;
    }

    private void InitializeInput()
    {
        _inputContext = SilkInputWindowExtensions.CreateInput(_window);

        if (_inputContext is null)
        {
            return;
        }

        if (_inputContext.Keyboards.Count > 0)
        {
            _silkKeyboard = _inputContext.Keyboards[0];
            _silkKeyboard.KeyDown += OnSilkKeyDown;
            _silkKeyboard.KeyUp += OnSilkKeyUp;
        }

        if (_inputContext.Mice.Count > 0)
        {
            _silkMouse = _inputContext.Mice[0];
            _silkMouse.MouseDown += OnSilkMouseDown;
            _silkMouse.MouseUp += OnSilkMouseUp;
            _silkMouse.MouseMove += OnSilkMouseMove;
        }
    }

    private void OnSilkKeyDown(SilkInput.IKeyboard keyboard, SilkInput.Key key, int keyCode)
    {
        _gnosisKeyboard?.SetKeyState(MapKeyCode(key), true);
    }

    private void OnSilkKeyUp(SilkInput.IKeyboard keyboard, SilkInput.Key key, int keyCode)
    {
        _gnosisKeyboard?.SetKeyState(MapKeyCode(key), false);
    }

    private void OnSilkMouseDown(SilkInput.IMouse mouse, SilkInput.MouseButton button)
    {
        _gnosisMouse?.SetButtonState(MapMouseButton(button), true);
    }

    private void OnSilkMouseUp(SilkInput.IMouse mouse, SilkInput.MouseButton button)
    {
        _gnosisMouse?.SetButtonState(MapMouseButton(button), false);
    }

    private void OnSilkMouseMove(SilkInput.IMouse mouse, Vector2 position)
    {
        _gnosisMouse?.SetPosition(position.X, position.Y);
    }

    private static int MapKeyCode(SilkInput.Key key)
    {
        return key switch
        {
            SilkInput.Key.A => GnosisInputDevice.KeyCode.A,
            SilkInput.Key.D => GnosisInputDevice.KeyCode.D,
            SilkInput.Key.S => GnosisInputDevice.KeyCode.S,
            SilkInput.Key.W => GnosisInputDevice.KeyCode.W,
            SilkInput.Key.Left => GnosisInputDevice.KeyCode.Left,
            SilkInput.Key.Right => GnosisInputDevice.KeyCode.Right,
            SilkInput.Key.Up => GnosisInputDevice.KeyCode.Up,
            SilkInput.Key.Down => GnosisInputDevice.KeyCode.Down,
            SilkInput.Key.Space => GnosisInputDevice.KeyCode.Space,
            SilkInput.Key.Escape => GnosisInputDevice.KeyCode.Escape,
            SilkInput.Key.Enter => GnosisInputDevice.KeyCode.Enter,
            SilkInput.Key.ShiftLeft => GnosisInputDevice.KeyCode.LeftShift,
            SilkInput.Key.ControlLeft => GnosisInputDevice.KeyCode.LeftControl,
            SilkInput.Key.F1 => GnosisInputDevice.KeyCode.F1,
            SilkInput.Key.F2 => GnosisInputDevice.KeyCode.F2,
            SilkInput.Key.F3 => GnosisInputDevice.KeyCode.F3,
            SilkInput.Key.F4 => GnosisInputDevice.KeyCode.F4,
            SilkInput.Key.F5 => GnosisInputDevice.KeyCode.F5,
            SilkInput.Key.F6 => GnosisInputDevice.KeyCode.F6,
            SilkInput.Key.F7 => GnosisInputDevice.KeyCode.F7,
            SilkInput.Key.F8 => GnosisInputDevice.KeyCode.F8,
            SilkInput.Key.F9 => GnosisInputDevice.KeyCode.F9,
            SilkInput.Key.F10 => GnosisInputDevice.KeyCode.F10,
            SilkInput.Key.F11 => GnosisInputDevice.KeyCode.F11,
            SilkInput.Key.F12 => GnosisInputDevice.KeyCode.F12,
            SilkInput.Key.Number0 => 48,
            SilkInput.Key.Number1 => 49,
            SilkInput.Key.Number2 => 50,
            SilkInput.Key.Number3 => 51,
            SilkInput.Key.Number4 => 52,
            SilkInput.Key.Number5 => 53,
            SilkInput.Key.Number6 => 54,
            SilkInput.Key.Number7 => 55,
            SilkInput.Key.Number8 => 56,
            SilkInput.Key.Number9 => 57,
            _ => -1
        };
    }

    private static int MapMouseButton(SilkInput.MouseButton button)
    {
        return button switch
        {
            SilkInput.MouseButton.Left => GnosisInputDevice.MouseButton.Left,
            SilkInput.MouseButton.Right => GnosisInputDevice.MouseButton.Right,
            SilkInput.MouseButton.Middle => GnosisInputDevice.MouseButton.Middle,
            SilkInput.MouseButton.Button4 => GnosisInputDevice.MouseButton.Back,
            SilkInput.MouseButton.Button5 => GnosisInputDevice.MouseButton.Forward,
            _ => -1
        };
    }

    #endregion

    #region 窗口事件

    private void OnWindowResize(Silk.NET.Maths.Vector2D<int> size)
    {
        OnResize?.Invoke(EventArgs.Empty);
    }

    private void OnWindowClosing()
    {
        OnClosing?.Invoke(EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (GlContext is SilkGL.GL gl)
        {
            gl.Dispose();
        }

        _inputContext?.Dispose();
        _window.Reset();

        _isDisposed = true;
    }

    #endregion
}
