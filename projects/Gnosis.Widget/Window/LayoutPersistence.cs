using System.Text.Json;
using System.Text.Json.Serialization;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;
using Gnosis.Widget.Style;

namespace Gnosis.Widget.Window;

public sealed class LayoutPersistence
{
    #region 内部类型

    public sealed class WindowLayoutData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "Untitled";

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("width")]
        public float Width { get; set; } = 300;

        [JsonPropertyName("height")]
        public float Height { get; set; } = 400;

        [JsonPropertyName("state")]
        public string State { get; set; } = "Floating";

        [JsonPropertyName("dockZone")]
        public string? DockZone { get; set; }

        [JsonPropertyName("tabIndex")]
        public int TabIndex { get; set; } = -1;

        [JsonPropertyName("splitPosition")]
        public float SplitPosition { get; set; } = 0.5f;

        [JsonPropertyName("splitDirection")]
        public string? SplitDirection { get; set; }

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = true;

        [JsonPropertyName("zOrder")]
        public int ZOrder { get; set; }
    }

    public sealed class EditorLayoutData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "dark";

        [JsonPropertyName("canvasWidth")]
        public float CanvasWidth { get; set; } = 1280;

        [JsonPropertyName("canvasHeight")]
        public float CanvasHeight { get; set; } = 720;

        [JsonPropertyName("windows")]
        public List<WindowLayoutData> Windows { get; set; } = [];
    }

    #endregion

    #region 字段

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _basePath;

    #endregion

    #region 属性

    public string CurrentLayoutName { get; private set; } = "default";

    #endregion

    #region 事件

    public event Action<string>? LayoutSaved;
    public event Action<string>? LayoutLoaded;

    #endregion

    #region 构造

    public LayoutPersistence(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gnosis", "Editor", "Layouts"
        );
    }

    #endregion

    #region 保存布局

    public void Save(WindowManager windowManager, ThemeManager themeManager, string layoutName = "default")
    {
        CurrentLayoutName = layoutName;

        var data = CaptureLayout(windowManager, themeManager);
        var json = JsonSerializer.Serialize(data, JsonOptions);

        Directory.CreateDirectory(_basePath);
        var filePath = GetLayoutFilePath(layoutName);
        File.WriteAllText(filePath, json);

        LayoutSaved?.Invoke(layoutName);
    }

    public EditorLayoutData CaptureLayout(WindowManager windowManager, ThemeManager themeManager)
    {
        var data = new EditorLayoutData
        {
            Theme = themeManager.CurrentTheme?.Name ?? "dark"
        };

        for (var i = 0; i < windowManager.Windows.Count; i++)
        {
            var pane = windowManager.Windows[i];
            var windowData = new WindowLayoutData
            {
                Id = pane.Id ?? $"window_{i}",
                Title = pane.Title,
                X = pane.X,
                Y = pane.Y,
                Width = pane.Width ?? 300,
                Height = pane.Height ?? 400,
                State = pane.State.ToString(),
                IsVisible = pane.Visibility == Visibility.Visible,
                ZOrder = i
            };

            if (pane.State == WindowState.Docked)
            {
                windowData.DockZone = FindDockZone(pane);
            }

            data.Windows.Add(windowData);
        }

        return data;
    }

    #endregion

    #region 加载布局

    public EditorLayoutData? Load(string layoutName = "default")
    {
        CurrentLayoutName = layoutName;

        var filePath = GetLayoutFilePath(layoutName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);

        try
        {
            var data = JsonSerializer.Deserialize<EditorLayoutData>(json, JsonOptions);
            LayoutLoaded?.Invoke(layoutName);
            return data;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public bool ApplyLayout(EditorLayoutData data, WindowManager windowManager, ThemeManager themeManager)
    {
        if (data.Version != 1)
        {
            return false;
        }

        themeManager.SetTheme(data.Theme);

        foreach (var existingWindow in windowManager.Windows.ToList())
        {
            windowManager.RemoveWindow(existingWindow);
        }

        var sortedWindows = data.Windows.OrderBy(w => w.ZOrder).ToList();

        foreach (var windowData in sortedWindows)
        {
            if (!windowData.IsVisible)
            {
                continue;
            }

            var content = new VBox
            {
                Background = new Color(0.15f, 0.15f, 0.17f, 1.0f),
                Padding = new EdgeInsets(10, 10, 10, 10)
            };

            content.AddChild(new Render.TextWidget(windowData.Title)
            {
                FontSize = 14,
                Foreground = new Color(0.9f, 0.9f, 0.9f)
            });

            var pane = windowManager.CreateWindow(
                windowData.Title,
                content,
                windowData.X,
                windowData.Y,
                windowData.Width,
                windowData.Height
            );

            pane.Id = windowData.Id;

            if (Enum.TryParse<WindowState>(windowData.State, out var state))
            {
                pane.State = state;
            }
        }

        return true;
    }

    #endregion

    #region 布局管理

    public IReadOnlyList<string> GetAvailableLayouts()
    {
        if (!Directory.Exists(_basePath))
        {
            return [];
        }

        return Directory.GetFiles(_basePath, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n)
            .ToList();
    }

    public bool DeleteLayout(string layoutName)
    {
        var filePath = GetLayoutFilePath(layoutName);

        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    public bool LayoutExists(string layoutName)
    {
        return File.Exists(GetLayoutFilePath(layoutName));
    }

    #endregion

    #region 辅助方法

    private string GetLayoutFilePath(string layoutName)
    {
        var safeName = string.Join("_", layoutName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"{safeName}.json");
    }

    private static string? FindDockZone(WindowPane pane)
    {
        if (pane.Parent is DockPanel dockPanel)
        {
            foreach (var child in dockPanel.Children)
            {
                if (child is DockLayoutInfo dockInfo && dockInfo.Child == pane)
                {
                    return dockInfo.Zone.ToString();
                }
            }
        }

        return null;
    }

    #endregion
}
