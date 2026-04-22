using System.Collections.Concurrent;
using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;

namespace Gnosis.Widget;

/// <summary>
/// 控制台面板，显示日志消息并支持日志级别过滤和命令输入
/// </summary>
public sealed class ConsolePanel : ContainerElement
{
    #region 常量

    private const int MaxLogEntries = 1000;
    private const int DefaultVisibleLines = 100;

    #endregion

    #region 字段

    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly List<LogEntry> _filteredEntries = new();
    private readonly HashSet<LogLevel> _enabledLevels = new()
    {
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Info,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Fatal
    };

    private string _searchFilter = "";
    private bool _autoScroll = true;
    private float _scrollY;
    private float _contentHeight;
    private string _commandInput = "";
    private float _commandCursorPos;
    private bool _commandFocused;
    private int _selectedIndex = -1;

    #endregion

    #region 属性

    /// <summary>
    /// 日志条目总数
    /// </summary>
    public int LogCount => _logEntries.Count;

    /// <summary>
    /// 过滤后的日志条目数
    /// </summary>
    public int FilteredLogCount => _filteredEntries.Count;

    /// <summary>
    /// 是否自动滚动到底部
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    /// <summary>
    /// 搜索过滤关键词
    /// </summary>
    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            _searchFilter = value ?? "";
            RebuildFilteredEntries();
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// 字体大小
    /// </summary>
    public float FontSize { get; set; } = 12;

    /// <summary>
    /// 行高
    /// </summary>
    public float LineHeight => FontSize * 1.4f;

    /// <summary>
    /// 命令输入框高度
    /// </summary>
    public float CommandBarHeight { get; set; } = 28;

    /// <summary>
    /// 工具栏高度
    /// </summary>
    public float ToolbarHeight { get; set; } = 24;

    /// <summary>
    /// 面板标题
    /// </summary>
    public string Title { get; set; } = "Console";

    /// <summary>
    /// 面板背景色
    /// </summary>
    public Color PanelBackground { get; set; } = new(0.12f, 0.12f, 0.14f, 1.0f);

    /// <summary>
    /// 工具栏背景色
    /// </summary>
    public Color ToolbarBackground { get; set; } = new(0.16f, 0.16f, 0.18f, 1.0f);

    /// <summary>
    /// 命令栏背景色
    /// </summary>
    public Color CommandBarBackground { get; set; } = new(0.14f, 0.14f, 0.16f, 1.0f);

    /// <summary>
    /// 命令栏边框色
    /// </summary>
    public Color CommandBarBorder { get; set; } = new(0.30f, 0.30f, 0.34f, 1.0f);

    /// <summary>
    /// 命令栏焦点边框色
    /// </summary>
    public Color CommandBarFocusBorder { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    /// <summary>
    /// 默认文字颜色
    /// </summary>
    public Color DefaultTextColor { get; set; } = new(0.85f, 0.85f, 0.88f, 1.0f);

    /// <summary>
    /// 时间戳颜色
    /// </summary>
    public Color TimestampColor { get; set; } = new(0.50f, 0.50f, 0.55f, 1.0f);

    /// <summary>
    /// 搜索框占位符颜色
    /// </summary>
    public Color PlaceholderColor { get; set; } = new(0.40f, 0.40f, 0.45f, 1.0f);

    /// <summary>
    /// 选中行背景色
    /// </summary>
    public Color SelectedLineBackground { get; set; } = new(0.20f, 0.30f, 0.50f, 0.40f);

    /// <summary>
    /// 命令提示符颜色
    /// </summary>
    public Color PromptColor { get; set; } = new(0.50f, 0.80f, 0.50f, 1.0f);

    #endregion

    #region 事件

    /// <summary>
    /// 命令提交时触发
    /// </summary>
    public event EventHandler<ConsoleCommandEventArgs>? CommandSubmitted;

    /// <summary>
    /// 日志条目点击时触发
    /// </summary>
    public event EventHandler<LogEntryEventArgs>? LogEntryClicked;

    #endregion

    #region 日志记录方法

    /// <summary>
    /// 记录 Trace 级别日志
    /// </summary>
    public void Trace(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Trace, message, source);
    }

    /// <summary>
    /// 记录 Debug 级别日志
    /// </summary>
    public void Debug(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Debug, message, source);
    }

    /// <summary>
    /// 记录 Info 级别日志
    /// </summary>
    public void Info(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Info, message, source);
    }

    /// <summary>
    /// 记录 Warning 级别日志
    /// </summary>
    public void Warning(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Warning, message, source);
    }

    /// <summary>
    /// 记录 Error 级别日志
    /// </summary>
    public void Error(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Error, message, source);
    }

    /// <summary>
    /// 记录 Fatal 级别日志
    /// </summary>
    public void Fatal(string message, string? source = null)
    {
        AddLogEntry(LogLevel.Fatal, message, source);
    }

    /// <summary>
    /// 添加日志条目
    /// </summary>
    public void AddLogEntry(LogLevel level, string message, string? source = null)
    {
        var entry = new LogEntry(level, message, source, DateTime.Now);

        while (_logEntries.Count >= MaxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }

        _logEntries.Enqueue(entry);

        if (_enabledLevels.Contains(level) && MatchesSearchFilter(entry))
        {
            _filteredEntries.Add(entry);
        }

        if (_autoScroll)
        {
            ScrollToBottom();
        }

        InvalidateMeasure();
    }

    /// <summary>
    /// 清除所有日志
    /// </summary>
    public void Clear()
    {
        while (_logEntries.TryDequeue(out _)) { }
        _filteredEntries.Clear();
        _scrollY = 0;
        _contentHeight = 0;
        InvalidateMeasure();
    }

    #endregion

    #region 日志级别过滤

    /// <summary>
    /// 启用指定日志级别
    /// </summary>
    public void EnableLogLevel(LogLevel level)
    {
        _enabledLevels.Add(level);
        RebuildFilteredEntries();
        InvalidateMeasure();
    }

    /// <summary>
    /// 禁用指定日志级别
    /// </summary>
    public void DisableLogLevel(LogLevel level)
    {
        _enabledLevels.Remove(level);
        RebuildFilteredEntries();
        InvalidateMeasure();
    }

    /// <summary>
    /// 判断指定日志级别是否启用
    /// </summary>
    public bool IsLogLevelEnabled(LogLevel level)
    {
        return _enabledLevels.Contains(level);
    }

    /// <summary>
    /// 设置日志级别启用状态
    /// </summary>
    public void SetLogLevelEnabled(LogLevel level, bool enabled)
    {
        if (enabled)
        {
            EnableLogLevel(level);
        }
        else
        {
            DisableLogLevel(level);
        }
    }

    #endregion

    #region 滚动

    /// <summary>
    /// 滚动到顶部
    /// </summary>
    public void ScrollToTop()
    {
        _scrollY = 0;
        _autoScroll = false;
        InvalidateArrange();
    }

    /// <summary>
    /// 滚动到底部
    /// </summary>
    public void ScrollToBottom()
    {
        _autoScroll = true;
        InvalidateArrange();
    }

    /// <summary>
    /// 按行数滚动
    /// </summary>
    public void ScrollByLines(int lines)
    {
        _scrollY += lines * LineHeight;
        _scrollY = Math.Max(0, _scrollY);
        _autoScroll = false;
        InvalidateArrange();
    }

    #endregion

    #region 命令输入

    /// <summary>
    /// 提交命令
    /// </summary>
    public void SubmitCommand()
    {
        if (string.IsNullOrWhiteSpace(_commandInput))
        {
            return;
        }

        CommandSubmitted?.Invoke(this, new ConsoleCommandEventArgs(_commandInput));

        Info($"> {_commandInput}");

        _commandInput = "";
        _commandCursorPos = 0;
    }

    /// <summary>
    /// 设置命令输入文本
    /// </summary>
    public void SetCommandInput(string text)
    {
        _commandInput = text ?? "";
        _commandCursorPos = _commandInput.Length;
    }

    /// <summary>
    /// 设置命令输入框焦点状态
    /// </summary>
    public void SetCommandFocus(bool focused)
    {
        _commandFocused = focused;
        InvalidateArrange();
    }

    /// <summary>
    /// 选中指定索引的日志条目
    /// </summary>
    public void SelectLogEntry(int index)
    {
        if (index < 0 || index >= _filteredEntries.Count)
        {
            _selectedIndex = -1;
            return;
        }

        _selectedIndex = index;
        LogEntryClicked?.Invoke(this, new LogEntryEventArgs(_filteredEntries[index]));
        InvalidateArrange();
    }

    /// <summary>
    /// 获取指定索引的过滤后日志条目
    /// </summary>
    public LogEntry? GetFilteredEntry(int index)
    {
        if (index < 0 || index >= _filteredEntries.Count)
        {
            return null;
        }

        return _filteredEntries[index];
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        _contentHeight = _filteredEntries.Count * LineHeight;

        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        renderer.DrawRect(
            contentRect.X, contentRect.Y,
            contentRect.Width, contentRect.Height,
            PanelBackground.R, PanelBackground.G, PanelBackground.B, PanelBackground.A
        );

        PaintToolbar(renderer, contentRect);

        var logAreaRect = new Rect(
            contentRect.X,
            contentRect.Y + ToolbarHeight,
            contentRect.Width,
            contentRect.Height - ToolbarHeight - CommandBarHeight
        );

        PaintLogArea(renderer, logAreaRect);

        PaintCommandBar(renderer, contentRect);
    }

    private void PaintToolbar(IWidgetRenderer renderer, Rect contentRect)
    {
        renderer.DrawRect(
            contentRect.X, contentRect.Y,
            contentRect.Width, ToolbarHeight,
            ToolbarBackground.R, ToolbarBackground.G, ToolbarBackground.B, ToolbarBackground.A
        );

        var x = contentRect.X + 6;
        var y = contentRect.Y + (ToolbarHeight - FontSize) / 2;

        renderer.DrawText("Clear", x, y, FontSize, DefaultTextColor.R, DefaultTextColor.G, DefaultTextColor.B);
        x += 40;

        PaintLogLevelToggles(renderer, ref x, y);

        if (!string.IsNullOrEmpty(_searchFilter))
        {
            var filterText = $"Filter: {_searchFilter}";
            var filterColor = new Color(0.6f, 0.8f, 1.0f, 1.0f);
            renderer.DrawText(filterText, x, y, FontSize, filterColor.R, filterColor.G, filterColor.B);
        }
    }

    private void PaintLogLevelToggles(IWidgetRenderer renderer, ref float x, float y)
    {
        var levels = new[]
        {
            (LogLevel.Trace, "T", new Color(0.6f, 0.6f, 0.6f, 1.0f)),
            (LogLevel.Debug, "D", new Color(0.6f, 0.6f, 0.6f, 1.0f)),
            (LogLevel.Info, "I", new Color(0.4f, 0.7f, 1.0f, 1.0f)),
            (LogLevel.Warning, "W", new Color(1.0f, 0.85f, 0.3f, 1.0f)),
            (LogLevel.Error, "E", new Color(1.0f, 0.3f, 0.3f, 1.0f)),
            (LogLevel.Fatal, "F", new Color(1.0f, 0.1f, 0.1f, 1.0f))
        };

        foreach (var (level, label, color) in levels)
        {
            var isEnabled = _enabledLevels.Contains(level);
            var alpha = isEnabled ? 1.0f : 0.3f;

            renderer.DrawText(label, x, y, FontSize, color.R, color.G, color.B * alpha);
            x += 16;
        }

        x += 8;
    }

    private void PaintLogArea(IWidgetRenderer renderer, Rect logAreaRect)
    {
        renderer.PushClip(logAreaRect.X, logAreaRect.Y, logAreaRect.Width, logAreaRect.Height);

        var viewHeight = logAreaRect.Height;
        var maxScrollY = Math.Max(0, _contentHeight - viewHeight);

        if (_autoScroll)
        {
            _scrollY = maxScrollY;
        }
        else
        {
            _scrollY = Math.Clamp(_scrollY, 0, maxScrollY);
        }

        var firstVisibleLine = (int)(_scrollY / LineHeight);
        var visibleLineCount = (int)(viewHeight / LineHeight) + 2;
        var lastVisibleLine = Math.Min(firstVisibleLine + visibleLineCount, _filteredEntries.Count);

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            if (i < 0 || i >= _filteredEntries.Count)
            {
                continue;
            }

            var entry = _filteredEntries[i];
            var lineY = logAreaRect.Y + i * LineHeight - _scrollY;

            PaintLogEntryBackground(renderer, entry, logAreaRect.X, lineY, logAreaRect.Width, LineHeight);
            PaintLogEntryText(renderer, entry, logAreaRect.X + 4, lineY + 2);
        }

        renderer.PopClip();

        PaintScrollBar(renderer, logAreaRect, maxScrollY);
    }

    private void PaintLogEntryBackground(IWidgetRenderer renderer, LogEntry entry, float x, float y, float width, float height)
    {
        if (entry.Level == LogLevel.Error || entry.Level == LogLevel.Fatal)
        {
            var bgColor = entry.Level == LogLevel.Fatal
                ? new Color(0.4f, 0.0f, 0.0f, 0.3f)
                : new Color(0.3f, 0.0f, 0.0f, 0.2f);

            renderer.DrawRect(x, y, width, height, bgColor.R, bgColor.G, bgColor.B, bgColor.A);
        }
        else if (entry.Level == LogLevel.Warning)
        {
            var bgColor = new Color(0.3f, 0.25f, 0.0f, 0.15f);
            renderer.DrawRect(x, y, width, height, bgColor.R, bgColor.G, bgColor.B, bgColor.A);
        }
    }

    private void PaintLogEntryText(IWidgetRenderer renderer, LogEntry entry, float x, float y)
    {
        var timestampText = $"[{entry.Timestamp:HH:mm:ss.fff}]";
        renderer.DrawText(timestampText, x, y, FontSize, TimestampColor.R, TimestampColor.G, TimestampColor.B);

        x += timestampText.Length * FontSize * 0.6f + 4;

        var levelText = GetLevelPrefix(entry.Level);
        var levelColor = GetLevelColor(entry.Level);
        renderer.DrawText(levelText, x, y, FontSize, levelColor.R, levelColor.G, levelColor.B);

        x += levelText.Length * FontSize * 0.6f + 4;

        if (!string.IsNullOrEmpty(entry.Source))
        {
            var sourceText = $"[{entry.Source}]";
            var sourceColor = new Color(0.5f, 0.7f, 0.9f, 1.0f);
            renderer.DrawText(sourceText, x, y, FontSize, sourceColor.R, sourceColor.G, sourceColor.B);
            x += sourceText.Length * FontSize * 0.6f + 4;
        }

        var messageColor = entry.Level >= LogLevel.Error
            ? new Color(1.0f, 0.6f, 0.6f, 1.0f)
            : DefaultTextColor;

        renderer.DrawText(entry.Message, x, y, FontSize, messageColor.R, messageColor.G, messageColor.B);
    }

    private void PaintScrollBar(IWidgetRenderer renderer, Rect logAreaRect, float maxScrollY)
    {
        if (_contentHeight <= logAreaRect.Height)
        {
            return;
        }

        var scrollBarWidth = 8;
        var trackX = logAreaRect.Right - scrollBarWidth;

        renderer.DrawRect(
            trackX, logAreaRect.Y,
            scrollBarWidth, logAreaRect.Height,
            0.20f, 0.20f, 0.22f, 0.60f
        );

        var thumbRatio = logAreaRect.Height / _contentHeight;
        var thumbHeight = Math.Max(20, logAreaRect.Height * thumbRatio);
        var thumbY = logAreaRect.Y + (_scrollY / maxScrollY) * (logAreaRect.Height - thumbHeight);

        renderer.DrawRect(
            trackX + 1, thumbY,
            scrollBarWidth - 2, thumbHeight,
            0.45f, 0.45f, 0.48f, 0.70f
        );
    }

    private void PaintCommandBar(IWidgetRenderer renderer, Rect contentRect)
    {
        var barY = contentRect.Bottom - CommandBarHeight;

        renderer.DrawRect(
            contentRect.X, barY,
            contentRect.Width, CommandBarHeight,
            CommandBarBackground.R, CommandBarBackground.G, CommandBarBackground.B, CommandBarBackground.A
        );

        var borderColor = _commandFocused ? CommandBarFocusBorder : CommandBarBorder;

        renderer.DrawRect(
            contentRect.X, barY,
            contentRect.Width, 1,
            borderColor.R, borderColor.G, borderColor.B, borderColor.A
        );

        var textY = barY + (CommandBarHeight - FontSize) / 2;
        var textX = contentRect.X + 6;

        renderer.DrawText(">", textX, textY, FontSize, PromptColor.R, PromptColor.G, PromptColor.B);
        textX += FontSize * 0.6f + 4;

        if (string.IsNullOrEmpty(_commandInput))
        {
            renderer.DrawText("Enter command...", textX, textY, FontSize, PlaceholderColor.R, PlaceholderColor.G, PlaceholderColor.B);
        }
        else
        {
            renderer.DrawText(_commandInput, textX, textY, FontSize, DefaultTextColor.R, DefaultTextColor.G, DefaultTextColor.B);
        }

        if (_commandFocused)
        {
            var cursorX = textX + _commandCursorPos * FontSize * 0.6f;
            renderer.DrawLine(
                cursorX, textY + 2,
                cursorX, textY + FontSize,
                0.9f, 0.9f, 0.92f
            );
        }
    }

    #endregion

    #region 辅助方法

    private static string GetLevelPrefix(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "[TRC]",
            LogLevel.Debug => "[DBG]",
            LogLevel.Info => "[INF]",
            LogLevel.Warning => "[WRN]",
            LogLevel.Error => "[ERR]",
            LogLevel.Fatal => "[FTL]",
            _ => "[???]"
        };
    }

    private Color GetLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => new Color(0.5f, 0.5f, 0.5f, 1.0f),
            LogLevel.Debug => new Color(0.6f, 0.6f, 0.6f, 1.0f),
            LogLevel.Info => new Color(0.4f, 0.7f, 1.0f, 1.0f),
            LogLevel.Warning => new Color(1.0f, 0.85f, 0.3f, 1.0f),
            LogLevel.Error => new Color(1.0f, 0.4f, 0.4f, 1.0f),
            LogLevel.Fatal => new Color(1.0f, 0.1f, 0.1f, 1.0f),
            _ => DefaultTextColor
        };
    }

    private void RebuildFilteredEntries()
    {
        _filteredEntries.Clear();

        foreach (var entry in _logEntries)
        {
            if (_enabledLevels.Contains(entry.Level) && MatchesSearchFilter(entry))
            {
                _filteredEntries.Add(entry);
            }
        }
    }

    private bool MatchesSearchFilter(LogEntry entry)
    {
        if (string.IsNullOrEmpty(_searchFilter))
        {
            return true;
        }

        return entry.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
               || (entry.Source?.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    #endregion
}

#region 日志数据类型

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

/// <summary>
/// 日志条目
/// </summary>
public sealed record LogEntry(LogLevel Level, string Message, string? Source, DateTime Timestamp);

/// <summary>
/// 控制台命令事件参数
/// </summary>
public sealed class ConsoleCommandEventArgs : EventArgs
{
    public string Command { get; }

    public ConsoleCommandEventArgs(string command)
    {
        Command = command;
    }
}

/// <summary>
/// 日志条目事件参数
/// </summary>
public sealed class LogEntryEventArgs : EventArgs
{
    public LogEntry Entry { get; }

    public LogEntryEventArgs(LogEntry entry)
    {
        Entry = entry;
    }
}

#endregion
