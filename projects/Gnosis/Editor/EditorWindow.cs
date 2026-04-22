using Gnosis.Rendering.Widget;
using UiColor = Gnosis.Rendering.Widget.Color;
using UiDock = Gnosis.Rendering.Widget.Dock;

namespace Gnosis.Editor;

public sealed class EditorWindow : Window
{
    private readonly IWidgetRenderer _uiRenderer;
    private readonly WidgetTreeRenderer _widgetRenderer;
    private readonly WriteableBitmap _bitmap;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    private float _time;
    private int _frameCount;

    private UiDock _root;

    public EditorWindow()
    {
        Title = "Gnosis Engine Editor";
        Width = 1280;
        Height = 720;
        Background = System.Windows.Media.Brushes.Black;

        int width = 1280;
        int height = 720;

        _uiRenderer = new UiRenderer(width, height);
        _widgetRenderer = new WidgetTreeRenderer((UiRenderer)_uiRenderer);
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

        var image = new System.Windows.Controls.Image
        {
            Source = _bitmap,
            Stretch = Stretch.Uniform
        };
        Content = image;

        BuildWidgetTree();

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void BuildWidgetTree()
    {
        _root = new UiDock();

        var menuBar = BuildMenuBar();
        _root.DockWidget(menuBar, DockPosition.Top);

        var statusBar = BuildStatusBar();
        _root.DockWidget(statusBar, DockPosition.Bottom);

        var body = new UiDock();

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
            Background = new UiColor(0.18f, 0.18f, 0.2f),
            Height = 30,
            Padding = new EdgeInsets(5, 10, 5, 10)
        };

        bar.AddChild(new TextWidget("File") { FontSize = 13, Foreground = new UiColor(0.9f, 0.9f, 0.9f) });
        bar.AddChild(new TextWidget("Edit") { FontSize = 13, Foreground = new UiColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });
        bar.AddChild(new TextWidget("View") { FontSize = 13, Foreground = new UiColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });
        bar.AddChild(new TextWidget("Help") { FontSize = 13, Foreground = new UiColor(0.9f, 0.9f, 0.9f), Margin = new EdgeInsets(0, 15, 0, 0) });

        return bar;
    }

    private HBox BuildStatusBar()
    {
        var bar = new HBox
        {
            Background = new UiColor(0.18f, 0.18f, 0.2f),
            Height = 24,
            Padding = new EdgeInsets(4, 10, 4, 10)
        };

        bar.AddChild(new TextWidget("FPS: --") { FontSize = 11, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Id = "fps" });
        bar.AddChild(new TextWidget(" | Shader: UI Uber Shader") { FontSize = 11, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(0, 10, 0, 0) });

        return bar;
    }

    private VBox BuildHierarchy()
    {
        var panel = new VBox
        {
            Background = new UiColor(0.15f, 0.15f, 0.17f),
            Width = 200,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Hierarchy") { FontSize = 14, Foreground = new UiColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("  Main Camera") { FontSize = 12, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Directional Light") { FontSize = 12, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Cube") { FontSize = 12, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Ground Plane") { FontSize = 12, Foreground = new UiColor(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(3, 0, 0, 0) });

        return panel;
    }

    private VBox BuildInspector()
    {
        var panel = new VBox
        {
            Background = new UiColor(0.15f, 0.15f, 0.17f),
            Width = 260,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Inspector") { FontSize = 14, Foreground = new UiColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("Transform") { FontSize = 12, Foreground = new UiColor(0.8f, 0.8f, 0.8f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Position: 0, 0, 0") { FontSize = 11, Foreground = new UiColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Rotation: 0, 0, 0") { FontSize = 11, Foreground = new UiColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Scale: 1, 1, 1") { FontSize = 11, Foreground = new UiColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });

        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(8, 0, 8, 0) });

        panel.AddChild(new TextWidget("Mesh Renderer") { FontSize = 12, Foreground = new UiColor(0.8f, 0.8f, 0.8f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Mesh: Cube") { FontSize = 11, Foreground = new UiColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("  Material: Default") { FontSize = 11, Foreground = new UiColor(0.6f, 0.6f, 0.6f), Margin = new EdgeInsets(3, 0, 0, 0) });

        return panel;
    }

    private UiDock BuildCenter()
    {
        var center = new UiDock();

        var viewport = new RectWidget
        {
            Background = new UiColor(0.1f, 0.1f, 0.12f),
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
            Background = new UiColor(0.12f, 0.12f, 0.14f),
            Height = 150,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Console") { FontSize = 14, Foreground = new UiColor(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        panel.AddChild(new TextWidget("[Info] Shader compiled successfully") { FontSize = 11, Foreground = new UiColor(0.4f, 0.8f, 0.4f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("[Render] Frame rendered in 0.5ms") { FontSize = 11, Foreground = new UiColor(0.4f, 0.6f, 0.9f), Margin = new EdgeInsets(3, 0, 0, 0) });
        panel.AddChild(new TextWidget("[Scene] Main scene loaded") { FontSize = 11, Foreground = new UiColor(0.9f, 0.9f, 0.4f), Margin = new EdgeInsets(3, 0, 0, 0) });

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
}
