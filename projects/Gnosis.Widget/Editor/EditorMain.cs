using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;
using Gnosis.Widget.Style;
using Gnosis.Widget.Window;

namespace Gnosis.Widget.Editor;

public sealed class EditorMain
{
    #region 字段

    private readonly ThemeManager _themeManager;
    private readonly StyleApplier _styleApplier;
    private readonly ShortcutManager _shortcutManager;
    private readonly WindowManager _windowManager;

    private WidgetElement? _root;
    private MenuBar? _menuBar;
    private ToolBar? _toolBar;

    #endregion

    #region 属性

    public ThemeManager ThemeManager => _themeManager;

    public ShortcutManager ShortcutManager => _shortcutManager;

    public WindowManager WindowManager => _windowManager;

    public WidgetElement? Root => _root;

    #endregion

    #region 事件

    public event Action? Initialized;
    public event Action? ShutdownRequested;

    #endregion

    #region 构造

    public EditorMain()
    {
        _themeManager = new ThemeManager();
        _styleApplier = new StyleApplier(_themeManager);
        _shortcutManager = new ShortcutManager();
        _windowManager = new WindowManager();

        RegisterDefaultShortcuts();
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        _root = BuildLayout();
        ApplyStyles();
        Initialized?.Invoke();
    }

    public void ApplyStyles()
    {
        if (_root == null)
        {
            return;
        }

        _styleApplier.ApplyAll(_root);
    }

    public MenuBar? GetMenuBar() => _menuBar;

    public ToolBar? GetToolBar() => _toolBar;

    public void RequestShutdown()
    {
        ShutdownRequested?.Invoke();
    }

    #endregion

    #region 布局构建

    private WidgetElement BuildLayout()
    {
        var root = new Dock();

        _menuBar = BuildMainMenu();
        root.DockWidget(_menuBar, DockPosition.Top);

        _toolBar = BuildMainToolBar();
        root.DockWidget(_toolBar, DockPosition.Top);

        var body = new Dock();

        var hierarchy = BuildHierarchyPanel();
        body.DockWidget(hierarchy, DockPosition.Left);

        var inspector = BuildInspectorPanel();
        body.DockWidget(inspector, DockPosition.Right);

        var center = BuildCenterArea();
        body.DockWidget(center, DockPosition.Fill);

        root.DockWidget(body, DockPosition.Fill);

        var statusBar = BuildStatusBar();
        root.DockWidget(statusBar, DockPosition.Bottom);

        return root;
    }

    private MenuBar BuildMainMenu()
    {
        var menuBar = new MenuBar
        {
            Height = 28,
            Background = new Color(0.18f, 0.18f, 0.20f, 1.0f)
        };

        var fileMenu = new MenuItem("File");
        fileMenu.AddChild(new MenuItem("New Project") { Shortcut = "Ctrl+N" });
        fileMenu.AddChild(new MenuItem("Open Project") { Shortcut = "Ctrl+O" });
        fileMenu.AddChild(new MenuItem("Save") { Shortcut = "Ctrl+S" });
        fileMenu.AddChild(new MenuItem("Save As...") { Shortcut = "Ctrl+Shift+S" });
        fileMenu.AddChild(MenuItem.Separator());
        fileMenu.AddChild(new MenuItem("Exit"));

        var editMenu = new MenuItem("Edit");
        editMenu.AddChild(new MenuItem("Undo") { Shortcut = "Ctrl+Z" });
        editMenu.AddChild(new MenuItem("Redo") { Shortcut = "Ctrl+Y" });
        editMenu.AddChild(MenuItem.Separator());
        editMenu.AddChild(new MenuItem("Cut") { Shortcut = "Ctrl+X" });
        editMenu.AddChild(new MenuItem("Copy") { Shortcut = "Ctrl+C" });
        editMenu.AddChild(new MenuItem("Paste") { Shortcut = "Ctrl+V" });
        editMenu.AddChild(MenuItem.Separator());
        editMenu.AddChild(new MenuItem("Select All") { Shortcut = "Ctrl+A" });

        var viewMenu = new MenuItem("View");
        viewMenu.AddChild(new MenuItem("Hierarchy"));
        viewMenu.AddChild(new MenuItem("Inspector"));
        viewMenu.AddChild(new MenuItem("Console"));
        viewMenu.AddChild(new MenuItem("Content Browser"));
        viewMenu.AddChild(MenuItem.Separator());
        viewMenu.AddChild(new MenuItem("Toggle Dark/Light Theme"));

        var helpMenu = new MenuItem("Help");
        helpMenu.AddChild(new MenuItem("Documentation"));
        helpMenu.AddChild(new MenuItem("About Gnosis"));

        menuBar.AddMenu(fileMenu);
        menuBar.AddMenu(editMenu);
        menuBar.AddMenu(viewMenu);
        menuBar.AddMenu(helpMenu);

        return menuBar;
    }

    private ToolBar BuildMainToolBar()
    {
        var toolBar = new ToolBar
        {
            Height = 32,
            Background = new Color(0.16f, 0.16f, 0.18f, 1.0f)
        };

        toolBar.AddItem(new ToolBarItem("Translate", "W"));
        toolBar.AddItem(new ToolBarItem("Rotate", "E"));
        toolBar.AddItem(new ToolBarItem("Scale", "R"));
        toolBar.AddSeparator();
        toolBar.AddItem(new ToolBarItem("Play"));
        toolBar.AddItem(new ToolBarItem("Pause"));
        toolBar.AddItem(new ToolBarItem("Stop"));
        toolBar.AddSeparator();
        toolBar.AddItem(new ToolBarItem("Layout"));

        return toolBar;
    }

    private static VBox BuildHierarchyPanel()
    {
        var panel = new VBox
        {
            Background = new Color(0.15f, 0.15f, 0.17f, 1.0f),
            Width = 200,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Hierarchy") { FontSize = 14, Foreground = new Color(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        return panel;
    }

    private static VBox BuildInspectorPanel()
    {
        var panel = new VBox
        {
            Background = new Color(0.15f, 0.15f, 0.17f, 1.0f),
            Width = 260,
            Padding = new EdgeInsets(10, 10, 10, 10),
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };

        panel.AddChild(new TextWidget("Inspector") { FontSize = 14, Foreground = new Color(0.9f, 0.9f, 0.9f) });
        panel.AddChild(new SeparatorWidget { Margin = new EdgeInsets(5, 0, 5, 0) });

        return panel;
    }

    private static Dock BuildCenterArea()
    {
        var center = new Dock();

        var viewport = new RectWidget
        {
            Background = new Color(0.10f, 0.10f, 0.12f, 1.0f),
            Id = "viewport"
        };
        center.DockWidget(viewport, DockPosition.Fill);

        return center;
    }

    private static HBox BuildStatusBar()
    {
        var bar = new HBox
        {
            Background = new Color(0.18f, 0.18f, 0.20f, 1.0f),
            Height = 24,
            Padding = new EdgeInsets(4, 10, 4, 10)
        };

        bar.AddChild(new TextWidget("FPS: --") { FontSize = 11, Foreground = new Color(0.7f, 0.7f, 0.7f), Id = "fps" });
        bar.AddChild(new TextWidget(" | Gnosis Engine") { FontSize = 11, Foreground = new Color(0.7f, 0.7f, 0.7f), Margin = new EdgeInsets(0, 10, 0, 0) });

        return bar;
    }

    #endregion

    #region 快捷键注册

    private void RegisterDefaultShortcuts()
    {
        _shortcutManager.Register("Ctrl+S", "Save", () => { });
        _shortcutManager.Register("Ctrl+Z", "Undo", () => { });
        _shortcutManager.Register("Ctrl+Y", "Redo", () => { });
        _shortcutManager.Register("Ctrl+N", "NewProject", () => { });
        _shortcutManager.Register("Ctrl+O", "OpenProject", () => { });
        _shortcutManager.Register("W", "TranslateTool", () => { });
        _shortcutManager.Register("E", "RotateTool", () => { });
        _shortcutManager.Register("R", "ScaleTool", () => { });
        _shortcutManager.Register("F5", "Play", () => { });
        _shortcutManager.Register("F6", "Pause", () => { });
        _shortcutManager.Register("F7", "Stop", () => { });
    }

    #endregion
}
