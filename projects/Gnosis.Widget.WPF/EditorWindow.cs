using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;
using WpfKey = System.Windows.Input.Key;
using WpfMouse = System.Windows.Input.MouseButton;
using WpfModifiers = System.Windows.Input.ModifierKeys;
using WidgetColor = Gnosis.Widget.Element.Color;
using WidgetKey = Gnosis.Widget.Element.Key;
using WidgetMouse = Gnosis.Widget.Element.MouseButton;
using WidgetKeyModifiers = Gnosis.Widget.Element.KeyModifiers;
using WidgetMouseEventArgs = Gnosis.Widget.Element.MouseEventArgs;
using WidgetKeyEventArgs = Gnosis.Widget.Element.KeyEventArgs;
using WidgetWheelEventArgs = Gnosis.Widget.Element.WheelEventArgs;
using WidgetVisibility = Gnosis.Widget.Element.Visibility;
using WidgetFocusManager = Gnosis.Widget.Element.FocusManager;

namespace Gnosis.Widget.WPF;

public sealed class EditorWindow : System.Windows.Window
{
    private readonly UiRenderer _uiRenderer;
    private readonly WidgetTreeRenderer _widgetRenderer;
    private readonly WriteableBitmap _bitmap;
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private readonly WidgetFocusManager _focusManager;
    private readonly EventRouter _eventRouter = new();

    private float _time;
    private int _frameCount;

    private Dock _root;

    private WidgetElement? _hoveredElement;

    public EditorWindow()
    {
        Title = "Gnosis Engine Editor";
        Width = 1280;
        Height = 720;
        Background = System.Windows.Media.Brushes.Black;

        int width = 1280;
        int height = 720;

        _uiRenderer = new UiRenderer(width, height);
        _widgetRenderer = new WidgetTreeRenderer(_uiRenderer);
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

        var image = new System.Windows.Controls.Image
        {
            Source = _bitmap,
            Stretch = Stretch.Uniform
        };
        Content = image;

        BuildWidgetTree();

        _focusManager = new WidgetFocusManager(_root);

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
        _timer.Start();

        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
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

    private void OnTick(object? sender, EventArgs e)
    {
        _time += 0.016f;
        _frameCount++;

        Render();

        unsafe
        {
            _bitmap.Lock();
            var data = _uiRenderer.GetFramebufferData();
            fixed (byte* src = data)
            {
                Buffer.MemoryCopy(src, _bitmap.BackBuffer.ToPointer(), data.Length, data.Length);
            }
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            _bitmap.Unlock();
        }

        Title = $"Gnosis Engine Editor - FPS: {_frameCount / _time:0} - Frame: {_frameCount}";
    }

    private void Render()
    {
        _uiRenderer.Clear(0.12f, 0.12f, 0.14f);
        _uiRenderer.Begin();

        _widgetRenderer.Render(_root, _uiRenderer.Width, _uiRenderer.Height);

        _uiRenderer.End();
    }

    #region 事件桥接

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        var x = (float)pos.X;
        var y = (float)pos.Y;

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

    private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        var button = ConvertMouseButton(e.ChangedButton);
        var target = HitTestTree(_root, (float)pos.X, (float)pos.Y);

        if (target != null)
        {
            var args = new WidgetMouseEventArgs((float)pos.X, (float)pos.Y, button);
            _eventRouter.RouteBubble(target, args);

            if (target.IsFocusable)
            {
                _focusManager.SetFocus(target);
            }
        }
    }

    private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        var button = ConvertMouseButton(e.ChangedButton);
        var target = HitTestTree(_root, (float)pos.X, (float)pos.Y);

        if (target != null)
        {
            var args = new WidgetMouseEventArgs((float)pos.X, (float)pos.Y, button);
            _eventRouter.RouteBubble(target, args);
        }
    }

    private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(this);
        var target = HitTestTree(_root, (float)pos.X, (float)pos.Y);

        if (target != null)
        {
            var args = new WidgetWheelEventArgs((float)pos.X, (float)pos.Y, e.Delta);
            _eventRouter.RouteBubble(target, args);
        }
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == WpfKey.Tab)
        {
            _focusManager.MoveNext();
            e.Handled = true;
            return;
        }

        var key = ConvertKey(e.Key);
        var modifiers = ConvertModifiers(System.Windows.Input.Keyboard.Modifiers);
        var args = new WidgetKeyEventArgs(key, modifiers);
        _eventRouter.RouteBubble(_focusManager.FocusedElement ?? _root, args);
    }

    private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = ConvertKey(e.Key);
        var modifiers = ConvertModifiers(System.Windows.Input.Keyboard.Modifiers);
        var args = new WidgetKeyEventArgs(key, modifiers);
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

    private static WidgetMouse ConvertMouseButton(WpfMouse button)
    {
        return button switch
        {
            WpfMouse.Left => WidgetMouse.Left,
            WpfMouse.Middle => WidgetMouse.Middle,
            WpfMouse.Right => WidgetMouse.Right,
            WpfMouse.XButton1 => WidgetMouse.XButton1,
            WpfMouse.XButton2 => WidgetMouse.XButton2,
            _ => WidgetMouse.None
        };
    }

    private static WidgetKey ConvertKey(WpfKey key)
    {
        return key switch
        {
            WpfKey.Back => WidgetKey.Back,
            WpfKey.Tab => WidgetKey.Tab,
            WpfKey.Enter => WidgetKey.Enter,
            WpfKey.Escape => WidgetKey.Escape,
            WpfKey.Space => WidgetKey.Space,
            WpfKey.Delete => WidgetKey.Delete,
            WpfKey.Left => WidgetKey.Left,
            WpfKey.Right => WidgetKey.Right,
            WpfKey.Up => WidgetKey.Up,
            WpfKey.Down => WidgetKey.Down,
            WpfKey.Home => WidgetKey.Home,
            WpfKey.End => WidgetKey.End,
            WpfKey.PageUp => WidgetKey.PageUp,
            WpfKey.PageDown => WidgetKey.PageDown,
            WpfKey.A => WidgetKey.A,
            WpfKey.B => WidgetKey.B,
            WpfKey.C => WidgetKey.C,
            WpfKey.D => WidgetKey.D,
            WpfKey.E => WidgetKey.E,
            WpfKey.F => WidgetKey.F,
            WpfKey.G => WidgetKey.G,
            WpfKey.H => WidgetKey.H,
            WpfKey.I => WidgetKey.I,
            WpfKey.J => WidgetKey.J,
            WpfKey.K => WidgetKey.K,
            WpfKey.L => WidgetKey.L,
            WpfKey.M => WidgetKey.M,
            WpfKey.N => WidgetKey.N,
            WpfKey.O => WidgetKey.O,
            WpfKey.P => WidgetKey.P,
            WpfKey.Q => WidgetKey.Q,
            WpfKey.R => WidgetKey.R,
            WpfKey.S => WidgetKey.S,
            WpfKey.T => WidgetKey.T,
            WpfKey.U => WidgetKey.U,
            WpfKey.V => WidgetKey.V,
            WpfKey.W => WidgetKey.W,
            WpfKey.X => WidgetKey.X,
            WpfKey.Y => WidgetKey.Y,
            WpfKey.Z => WidgetKey.Z,
            WpfKey.D0 => WidgetKey.D0,
            WpfKey.D1 => WidgetKey.D1,
            WpfKey.D2 => WidgetKey.D2,
            WpfKey.D3 => WidgetKey.D3,
            WpfKey.D4 => WidgetKey.D4,
            WpfKey.D5 => WidgetKey.D5,
            WpfKey.D6 => WidgetKey.D6,
            WpfKey.D7 => WidgetKey.D7,
            WpfKey.D8 => WidgetKey.D8,
            WpfKey.D9 => WidgetKey.D9,
            WpfKey.F1 => WidgetKey.F1,
            WpfKey.F2 => WidgetKey.F2,
            WpfKey.F3 => WidgetKey.F3,
            WpfKey.F4 => WidgetKey.F4,
            WpfKey.F5 => WidgetKey.F5,
            WpfKey.F6 => WidgetKey.F6,
            WpfKey.F7 => WidgetKey.F7,
            WpfKey.F8 => WidgetKey.F8,
            WpfKey.F9 => WidgetKey.F9,
            WpfKey.F10 => WidgetKey.F10,
            WpfKey.F11 => WidgetKey.F11,
            WpfKey.F12 => WidgetKey.F12,
            WpfKey.LeftShift => WidgetKey.Shift,
            WpfKey.RightShift => WidgetKey.Shift,
            WpfKey.LeftCtrl => WidgetKey.Control,
            WpfKey.RightCtrl => WidgetKey.Control,
            WpfKey.LeftAlt => WidgetKey.Alt,
            WpfKey.RightAlt => WidgetKey.Alt,
            _ => WidgetKey.None
        };
    }

    private static WidgetKeyModifiers ConvertModifiers(WpfModifiers modifiers)
    {
        var result = WidgetKeyModifiers.None;

        if (modifiers.HasFlag(WpfModifiers.Shift))
        {
            result |= WidgetKeyModifiers.Shift;
        }

        if (modifiers.HasFlag(WpfModifiers.Control))
        {
            result |= WidgetKeyModifiers.Control;
        }

        if (modifiers.HasFlag(WpfModifiers.Alt))
        {
            result |= WidgetKeyModifiers.Alt;
        }

        return result;
    }

    #endregion
}
