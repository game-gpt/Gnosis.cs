using System.Runtime.InteropServices;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;
using Sdl = Silk.NET.SDL.Sdl;
using Silk.NET.SDL;
using WidgetColor = Gnosis.Widget.Element.Color;
using WidgetKey = Gnosis.Widget.Element.Key;
using WidgetMouse = Gnosis.Widget.Element.MouseButton;
using WidgetKeyModifiers = Gnosis.Widget.Element.KeyModifiers;
using WidgetMouseEventArgs = Gnosis.Widget.Element.MouseEventArgs;
using WidgetKeyEventArgs = Gnosis.Widget.Element.KeyEventArgs;
using WidgetWheelEventArgs = Gnosis.Widget.Element.WheelEventArgs;
using WidgetVisibility = Gnosis.Widget.Element.Visibility;
using WidgetFocusManager = Gnosis.Widget.Element.FocusManager;

namespace Gnosis.Widget.SDL;

public sealed unsafe class SdlWindow : IDisposable
{
    private readonly UiRenderer _uiRenderer;
    private readonly WidgetTreeRenderer _widgetRenderer;
    private readonly WidgetFocusManager _focusManager;
    private readonly EventRouter _eventRouter = new();

    private readonly Sdl _sdl;
    private readonly Silk.NET.SDL.Window* _window;
    private readonly Renderer* _renderer;
    private readonly Texture* _texture;

    private readonly int _width;
    private readonly int _height;

    private float _time;
    private int _frameCount;
    private bool _running;

    private Dock _root = null!;
    private WidgetElement? _hoveredElement;

    public SdlWindow(int width = 1280, int height = 720)
    {
        _width = width;
        _height = height;

        _sdl = Sdl.GetApi();

        if (_sdl.Init(32) < 0)
        {
            throw new InvalidOperationException($"SDL 初始化失败: {_sdl.GetErrorS()}");
        }

        _window = _sdl.CreateWindow(
            "Gnosis Engine Editor",
            805240832,
            805240832,
            width,
            height,
            (uint)(4 | 32)
        );

        if (_window == null)
        {
            throw new InvalidOperationException($"SDL 窗口创建失败: {_sdl.GetErrorS()}");
        }

        _renderer = _sdl.CreateRenderer(_window, -1, 2);

        if (_renderer == null)
        {
            throw new InvalidOperationException($"SDL 渲染器创建失败: {_sdl.GetErrorS()}");
        }

        _texture = _sdl.CreateTexture(
            _renderer,
            362266913,
            1,
            width,
            height
        );

        if (_texture == null)
        {
            throw new InvalidOperationException($"SDL 纹理创建失败: {_sdl.GetErrorS()}");
        }

        _uiRenderer = new UiRenderer(width, height);
        _widgetRenderer = new WidgetTreeRenderer(_uiRenderer);

        BuildWidgetTree();

        _focusManager = new WidgetFocusManager(_root);
    }

    private void BuildWidgetTree()
    {
        _root = new Dock();

        var menuBar = BuildMenuBar();
        _root.DockWidget(menuBar, DockPosition.Top);

        var statusBar = BuildStatusBar();
        _root.DockWidget(statusBar, DockPosition.Bottom);

        var body = new Dock();

        var hierarchy = BuildHierarchy();
        body.DockWidget(hierarchy, DockPosition.Left);

        var inspector = BuildInspector();
        body.DockWidget(inspector, DockPosition.Right);

        var center = BuildCenter();
        body.DockWidget(center, DockPosition.Fill);

        _root.DockWidget(body, DockPosition.Fill);
    }

    private HBox BuildMenuBar()
    {
        var bar = new HBox
        {
            Background = new WidgetColor(0.18f, 0.18f, 0.2f),
            Height = 30,
            Padding = new EdgeInsets(5, 10, 5, 10)
        };

        bar.AddChild(new TextWidget("File") { FontSize = 13, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f) });
        bar.AddChild(new TextWidget("Edit") { FontSize = 13, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });
        bar.AddChild(new TextWidget("View") { FontSize = 13, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });
        bar.AddChild(new TextWidget("Help") { FontSize = 13, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });

        return bar;
    }

    private HBox BuildStatusBar()
    {
        var bar = new HBox
        {
            Background = new WidgetColor(0.18f, 0.18f, 0.2f),
            Height = 24,
            Padding = new EdgeInsets(4, 10, 4, 10)
        };

        bar.AddChild(new TextWidget("FPS: --") { FontSize = 11, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Id = "fps" });
        bar.AddChild(new TextWidget(" | Shader: UI Uber Shader") { FontSize = 11, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(0, 10, 0, 0) });

        return bar;
    }

    private VBox BuildHierarchy()
    {
        var panel = new VBox
        {
            Background = new WidgetColor(0.15f, 0.15f, 0.17f),
            Width = 200,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Hierarchy") { FontSize = 14, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("  Main Camera") { FontSize = 12, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Directional Light") { FontSize = 12, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Cube") { FontSize = 12, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Ground Plane") { FontSize = 12, Foreground = new WidgetColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });

        return panel;
    }

    private VBox BuildInspector()
    {
        var panel = new VBox
        {
            Background = new WidgetColor(0.15f, 0.15f, 0.17f),
            Width = 260,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Inspector") { FontSize = 14, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("Transform") { FontSize = 12, Foreground = new WidgetColor(0.8f, 0.8f, 0.8f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Position: 0, 0, 0") { FontSize = 11, Foreground = new WidgetColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Rotation: 0, 0, 0") { FontSize = 11, Foreground = new WidgetColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Scale: 1, 1, 1") { FontSize = 11, Foreground = new WidgetColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });

        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(8, 0, 8, 0) });

        panel.AddChild(new TextWidget("Mesh Renderer") { FontSize = 12, Foreground = new WidgetColor(0.8f, 0.8f, 0.8f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Mesh: Cube") { FontSize = 11, Foreground = new WidgetColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Material: Default") { FontSize = 11, Foreground = new WidgetColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });

        return panel;
    }

    private Dock BuildCenter()
    {
        var center = new Dock();

        var viewport = new RectWidget
        {
            Background = new WidgetColor(0.1f, 0.1f, 0.12f),
            Id = "viewport"
        };
        center.DockWidget(viewport, DockPosition.Fill);

        var console = BuildConsole();
        center.DockWidget(console, DockPosition.Bottom);

        return center;
    }

    private VBox BuildConsole()
    {
        var panel = new VBox
        {
            Background = new WidgetColor(0.12f, 0.12f, 0.14f),
            Height = 150,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Console") { FontSize = 14, Foreground = new WidgetColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("[Info] Shader compiled successfully") { FontSize = 11, Foreground = new WidgetColor(0.4f, 0.8f, 0.4f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("[Render] Frame rendered in 0.5ms") { FontSize = 11, Foreground = new WidgetColor(0.4f, 0.6f, 0.9f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("[Scene] Main scene loaded") { FontSize = 11, Foreground = new WidgetColor(0.9f, 0.9f, 0.4f), Margin = new EdgeInsets(3, 0, 0, 0) });

        return panel;
    }

    public void Run()
    {
        _running = true;
        var lastTime = DateTime.UtcNow;

        while (_running)
        {
            var currentTime = DateTime.UtcNow;
            var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
            lastTime = currentTime;

            _time += deltaTime;
            _frameCount++;

            ProcessEvents();

            Render();

            UpdateTexture();

            _sdl.RenderClear(_renderer);
            _sdl.RenderCopy(_renderer, _texture, null, null);
            _sdl.RenderPresent(_renderer);

            _sdl.SetWindowTitle(_window, $"Gnosis Engine Editor - FPS: {_frameCount / _time:0} - Frame: {_frameCount}");
        }
    }

    private void ProcessEvents()
    {
        Event sdlEvent = default;
        while (_sdl.PollEvent(ref sdlEvent) != 0)
        {
            switch ((EventType)sdlEvent.Type)
            {
                case EventType.Quit:
                    _running = false;
                    break;

                case EventType.Mousemotion:
                    OnMouseMove(sdlEvent.Motion);
                    break;

                case EventType.Mousebuttondown:
                    OnMouseDown(sdlEvent.Button);
                    break;

                case EventType.Mousebuttonup:
                    OnMouseUp(sdlEvent.Button);
                    break;

                case EventType.Mousewheel:
                    OnMouseWheel(sdlEvent.Wheel);
                    break;

                case EventType.Keydown:
                    OnKeyDown(sdlEvent.Key);
                    break;

                case EventType.Keyup:
                    OnKeyUp(sdlEvent.Key);
                    break;

                case EventType.Windowevent:
                    if (sdlEvent.Window.Event == (byte)WindowEventID.Close)
                    {
                        _running = false;
                    }
                    break;
            }
        }
    }

    private void Render()
    {
        _uiRenderer.Clear(0.12f, 0.12f, 0.14f);
        _uiRenderer.Begin();
        _widgetRenderer.Render(_root, _uiRenderer.Width, _uiRenderer.Height);
        _uiRenderer.End();
    }

    private void UpdateTexture()
    {
        var data = _uiRenderer.GetFramebufferData();

        void* pixels;
        int pitch;
        _sdl.LockTexture(_texture, null, &pixels, &pitch);

        var srcPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
        var dstPtr = pixels;

        for (int y = 0; y < _height; y++)
        {
            Buffer.MemoryCopy(
                (byte*)srcPtr + y * _width * 4,
                (byte*)dstPtr + y * pitch,
                _width * 4,
                _width * 4
            );
        }

        _sdl.UnlockTexture(_texture);
    }

    #region 事件处理

    private void OnMouseMove(MouseMotionEvent motion)
    {
        var x = motion.X;
        var y = motion.Y;

        var target = HitTestTree(_root, x, y);

        if (target != _hoveredElement)
        {
            if (_hoveredElement != null)
            {
                var leaveArgs = new WidgetMouseEventArgs(x, y);
                _eventRouter.RouteDirect(_hoveredElement, leaveArgs);
            }

            _hoveredElement = target;

            if (_hoveredElement != null)
            {
                var enterArgs = new WidgetMouseEventArgs(x, y);
                _eventRouter.RouteDirect(_hoveredElement, enterArgs);
            }
        }

        if (target != null)
        {
            var args = new WidgetMouseEventArgs(x, y);
            _eventRouter.RouteBubble(target, args);
        }
    }

    private void OnMouseDown(MouseButtonEvent button)
    {
        var mouseButton = ConvertMouseButton(button.Button);
        var target = HitTestTree(_root, button.X, button.Y);

        if (target != null)
        {
            var args = new WidgetMouseEventArgs(button.X, button.Y, mouseButton);
            _eventRouter.RouteBubble(target, args);

            if (target.IsFocusable)
            {
                _focusManager.SetFocus(target);
            }
        }
    }

    private void OnMouseUp(MouseButtonEvent button)
    {
        var mouseButton = ConvertMouseButton(button.Button);
        var target = HitTestTree(_root, button.X, button.Y);

        if (target != null)
        {
            var args = new WidgetMouseEventArgs(button.X, button.Y, mouseButton);
            _eventRouter.RouteBubble(target, args);
        }
    }

    private void OnMouseWheel(MouseWheelEvent wheel)
    {
        int x, y;
        _sdl.GetMouseState(&x, &y);
        var target = HitTestTree(_root, x, y);

        if (target != null)
        {
            var delta = (int)(wheel.Y * 120);
            var args = new WidgetWheelEventArgs(x, y, delta);
            _eventRouter.RouteBubble(target, args);
        }
    }

    private void OnKeyDown(KeyboardEvent key)
    {
        var widgetKey = ConvertKey(key.Keysym.Sym);

        if (widgetKey == WidgetKey.Tab)
        {
            _focusManager.MoveNext();
            return;
        }

        var modifiers = ConvertModifiers(key.Keysym.Mod);
        var args = new WidgetKeyEventArgs(widgetKey, modifiers);
        _eventRouter.RouteBubble(_focusManager.FocusedElement ?? _root, args);
    }

    private void OnKeyUp(KeyboardEvent key)
    {
        var widgetKey = ConvertKey(key.Keysym.Sym);
        var modifiers = ConvertModifiers(key.Keysym.Mod);
        var args = new WidgetKeyEventArgs(widgetKey, modifiers);
        _eventRouter.RouteBubble(_focusManager.FocusedElement ?? _root, args);
    }

    #endregion

    #region 辅助方法

    private static WidgetElement? HitTestTree(WidgetElement root, float x, float y)
    {
        if (!root.HitTest(x, y))
        {
            return null;
        }

        if (root is ContainerElement container)
        {
            for (var i = container.Children.Count - 1; i >= 0; i--)
            {
                var child = container.Children[i];
                if (child.Visibility == WidgetVisibility.Collapsed)
                {
                    continue;
                }

                var result = HitTestTree(child, x, y);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return root;
    }

    private static WidgetMouse ConvertMouseButton(byte button)
    {
        return button switch
        {
            1 => WidgetMouse.Left,
            2 => WidgetMouse.Middle,
            3 => WidgetMouse.Right,
            4 => WidgetMouse.XButton1,
            5 => WidgetMouse.XButton2,
            _ => WidgetMouse.None
        };
    }

    private static WidgetKey ConvertKey(int keycode)
    {
        return keycode switch
        {
            (int)KeyCode.KBackspace => WidgetKey.Back,
            (int)KeyCode.KTab => WidgetKey.Tab,
            (int)KeyCode.KReturn => WidgetKey.Enter,
            (int)KeyCode.KEscape => WidgetKey.Escape,
            (int)KeyCode.KSpace => WidgetKey.Space,
            (int)KeyCode.KDelete => WidgetKey.Delete,
            (int)KeyCode.KLeft => WidgetKey.Left,
            (int)KeyCode.KRight => WidgetKey.Right,
            (int)KeyCode.KUp => WidgetKey.Up,
            (int)KeyCode.KDown => WidgetKey.Down,
            (int)KeyCode.KHome => WidgetKey.Home,
            (int)KeyCode.KEnd => WidgetKey.End,
            (int)KeyCode.KPageup => WidgetKey.PageUp,
            (int)KeyCode.KPagedown => WidgetKey.PageDown,
            (int)KeyCode.KA => WidgetKey.A,
            (int)KeyCode.KB => WidgetKey.B,
            (int)KeyCode.KC => WidgetKey.C,
            (int)KeyCode.KD => WidgetKey.D,
            (int)KeyCode.KE => WidgetKey.E,
            (int)KeyCode.KF => WidgetKey.F,
            (int)KeyCode.KG => WidgetKey.G,
            (int)KeyCode.KH => WidgetKey.H,
            (int)KeyCode.KI => WidgetKey.I,
            (int)KeyCode.KJ => WidgetKey.J,
            (int)KeyCode.KK => WidgetKey.K,
            (int)KeyCode.KL => WidgetKey.L,
            (int)KeyCode.KM => WidgetKey.M,
            (int)KeyCode.KN => WidgetKey.N,
            (int)KeyCode.KO => WidgetKey.O,
            (int)KeyCode.KP => WidgetKey.P,
            (int)KeyCode.KQ => WidgetKey.Q,
            (int)KeyCode.KR => WidgetKey.R,
            (int)KeyCode.KS => WidgetKey.S,
            (int)KeyCode.KT => WidgetKey.T,
            (int)KeyCode.KU => WidgetKey.U,
            (int)KeyCode.KV => WidgetKey.V,
            (int)KeyCode.KW => WidgetKey.W,
            (int)KeyCode.KX => WidgetKey.X,
            (int)KeyCode.KY => WidgetKey.Y,
            (int)KeyCode.KZ => WidgetKey.Z,
            (int)KeyCode.K0 => WidgetKey.D0,
            (int)KeyCode.K1 => WidgetKey.D1,
            (int)KeyCode.K2 => WidgetKey.D2,
            (int)KeyCode.K3 => WidgetKey.D3,
            (int)KeyCode.K4 => WidgetKey.D4,
            (int)KeyCode.K5 => WidgetKey.D5,
            (int)KeyCode.K6 => WidgetKey.D6,
            (int)KeyCode.K7 => WidgetKey.D7,
            (int)KeyCode.K8 => WidgetKey.D8,
            (int)KeyCode.K9 => WidgetKey.D9,
            (int)KeyCode.KF1 => WidgetKey.F1,
            (int)KeyCode.KF2 => WidgetKey.F2,
            (int)KeyCode.KF3 => WidgetKey.F3,
            (int)KeyCode.KF4 => WidgetKey.F4,
            (int)KeyCode.KF5 => WidgetKey.F5,
            (int)KeyCode.KF6 => WidgetKey.F6,
            (int)KeyCode.KF7 => WidgetKey.F7,
            (int)KeyCode.KF8 => WidgetKey.F8,
            (int)KeyCode.KF9 => WidgetKey.F9,
            (int)KeyCode.KF10 => WidgetKey.F10,
            (int)KeyCode.KF11 => WidgetKey.F11,
            (int)KeyCode.KF12 => WidgetKey.F12,
            (int)KeyCode.KLshift => WidgetKey.Shift,
            (int)KeyCode.KRshift => WidgetKey.Shift,
            (int)KeyCode.KLctrl => WidgetKey.Control,
            (int)KeyCode.KRctrl => WidgetKey.Control,
            (int)KeyCode.KLalt => WidgetKey.Alt,
            (int)KeyCode.KRalt => WidgetKey.Alt,
            _ => WidgetKey.None
        };
    }

    private static WidgetKeyModifiers ConvertModifiers(ushort mod)
    {
        var result = WidgetKeyModifiers.None;

        if ((mod & (ushort)Keymod.Shift) != 0)
        {
            result |= WidgetKeyModifiers.Shift;
        }

        if ((mod & (ushort)Keymod.Ctrl) != 0)
        {
            result |= WidgetKeyModifiers.Control;
        }

        if ((mod & (ushort)Keymod.Alt) != 0)
        {
            result |= WidgetKeyModifiers.Alt;
        }

        return result;
    }

    #endregion

    public void Dispose()
    {
        if (_texture != null)
        {
            _sdl.DestroyTexture(_texture);
        }

        if (_renderer != null)
        {
            _sdl.DestroyRenderer(_renderer);
        }

        if (_window != null)
        {
            _sdl.DestroyWindow(_window);
        }

        _sdl.Quit();
    }
}
