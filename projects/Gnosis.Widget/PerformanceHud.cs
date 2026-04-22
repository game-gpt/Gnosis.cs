using System.Diagnostics;
using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget;

/// <summary>
/// 性能 HUD 控件，显示帧时间、Draw call、内存使用等性能指标
/// </summary>
public sealed class PerformanceHud : WidgetElement
{
    #region 常量

    private const int MaxFrameTimeSamples = 120;
    private const float WarningFrameTimeMs = 33.33f;
    private const float CriticalFrameTimeMs = 50.0f;

    #endregion

    #region 字段

    private readonly Stopwatch _frameStopwatch = new();
    private readonly float[] _frameTimeSamples = new float[MaxFrameTimeSamples];
    private int _frameTimeIndex;
    private int _frameCount;
    private long _lastGcMemory;
    private double _fpsUpdateIntervalMs = 500.0;
    private double _fpsAccumulator;
    private int _fpsFrameCount;
    private float _currentFps;
    private float _currentFrameTimeMs;
    private int _drawCallCount;
    private long _totalMemoryBytes;

    #endregion

    #region 属性

    /// <summary>
    /// 当前 FPS
    /// </summary>
    public float CurrentFps => _currentFps;

    /// <summary>
    /// 当前帧时间（毫秒）
    /// </summary>
    public float CurrentFrameTimeMs => _currentFrameTimeMs;

    /// <summary>
    /// 平均帧时间（毫秒）
    /// </summary>
    public float AverageFrameTimeMs
    {
        get
        {
            if (_frameCount == 0)
            {
                return 0;
            }

            var count = Math.Min(_frameCount, MaxFrameTimeSamples);
            float sum = 0;

            for (var i = 0; i < count; i++)
            {
                sum += _frameTimeSamples[i];
            }

            return sum / count;
        }
    }

    /// <summary>
    /// 最大帧时间（毫秒）
    /// </summary>
    public float MaxFrameTimeMs
    {
        get
        {
            if (_frameCount == 0)
            {
                return 0;
            }

            var count = Math.Min(_frameCount, MaxFrameTimeSamples);
            var max = 0f;

            for (var i = 0; i < count; i++)
            {
                if (_frameTimeSamples[i] > max)
                {
                    max = _frameTimeSamples[i];
                }
            }

            return max;
        }
    }

    /// <summary>
    /// Draw call 数量
    /// </summary>
    public int DrawCallCount
    {
        get => _drawCallCount;
        set => _drawCallCount = Math.Max(0, value);
    }

    /// <summary>
    /// 总内存使用量（字节）
    /// </summary>
    public long TotalMemoryBytes
    {
        get => _totalMemoryBytes;
        set => _totalMemoryBytes = Math.Max(0, value);
    }

    /// <summary>
    /// FPS 更新间隔（毫秒）
    /// </summary>
    public double FpsUpdateIntervalMs
    {
        get => _fpsUpdateIntervalMs;
        set => _fpsUpdateIntervalMs = Math.Max(100, value);
    }

    /// <summary>
    /// 是否显示帧时间图表
    /// </summary>
    public bool ShowFrameTimeGraph { get; set; } = true;

    /// <summary>
    /// 是否显示内存信息
    /// </summary>
    public bool ShowMemoryInfo { get; set; } = true;

    /// <summary>
    /// 是否显示 Draw call 信息
    /// </summary>
    public bool ShowDrawCallInfo { get; set; } = true;

    /// <summary>
    /// HUD 字体大小
    /// </summary>
    public float FontSize { get; set; } = 11;

    /// <summary>
    /// 帧时间图表高度
    /// </summary>
    public float GraphHeight { get; set; } = 40;

    /// <summary>
    /// HUD 背景色
    /// </summary>
    public Color HudBackground { get; set; } = new(0.0f, 0.0f, 0.0f, 0.75f);

    /// <summary>
    /// 正常状态文字颜色
    /// </summary>
    public Color NormalTextColor { get; set; } = new(0.6f, 0.9f, 0.6f, 1.0f);

    /// <summary>
    /// 警告状态文字颜色
    /// </summary>
    public Color WarningTextColor { get; set; } = new(1.0f, 0.85f, 0.3f, 1.0f);

    /// <summary>
    /// 严重状态文字颜色
    /// </summary>
    public Color CriticalTextColor { get; set; } = new(1.0f, 0.3f, 0.3f, 1.0f);

    /// <summary>
    /// 图表线条颜色
    /// </summary>
    public Color GraphLineColor { get; set; } = new(0.3f, 0.8f, 0.3f, 0.8f);

    /// <summary>
    /// 图表警告线颜色
    /// </summary>
    public Color GraphWarningColor { get; set; } = new(1.0f, 0.85f, 0.3f, 0.4f);

    /// <summary>
    /// 图表严重线颜色
    /// </summary>
    public Color GraphCriticalColor { get; set; } = new(1.0f, 0.3f, 0.3f, 0.4f);

    #endregion

    #region 构造

    public PerformanceHud()
    {
        _frameStopwatch.Start();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 标记新帧开始，应在每帧开始时调用
    /// </summary>
    public void BeginFrame()
    {
        _frameStopwatch.Restart();
    }

    /// <summary>
    /// 标记帧结束，应在每帧结束时调用
    /// </summary>
    public void EndFrame()
    {
        _frameStopwatch.Stop();

        var frameTimeMs = (float)_frameStopwatch.Elapsed.TotalMilliseconds;
        _currentFrameTimeMs = frameTimeMs;

        if (_frameCount < MaxFrameTimeSamples)
        {
            _frameTimeSamples[_frameCount] = frameTimeMs;
            _frameCount++;
        }
        else
        {
            _frameTimeSamples[_frameTimeIndex] = frameTimeMs;
            _frameTimeIndex = (_frameTimeIndex + 1) % MaxFrameTimeSamples;
        }

        _fpsAccumulator += frameTimeMs;
        _fpsFrameCount++;

        if (_fpsAccumulator >= _fpsUpdateIntervalMs)
        {
            _currentFps = (float)(_fpsFrameCount / (_fpsAccumulator / 1000.0));
            _fpsAccumulator = 0;
            _fpsFrameCount = 0;
        }

        UpdateMemoryStats();

        InvalidateMeasure();
    }

    /// <summary>
    /// 重置所有统计数据
    /// </summary>
    public void Reset()
    {
        _frameCount = 0;
        _frameTimeIndex = 0;
        _currentFps = 0;
        _currentFrameTimeMs = 0;
        _drawCallCount = 0;
        _totalMemoryBytes = 0;
        _fpsAccumulator = 0;
        _fpsFrameCount = 0;

        Array.Clear(_frameTimeSamples);

        InvalidateMeasure();
    }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        var lineCount = 1;

        if (ShowDrawCallInfo)
        {
            lineCount++;
        }

        if (ShowMemoryInfo)
        {
            lineCount++;
        }

        var textHeight = lineCount * (FontSize * 1.4f + 2);
        var graphHeight = ShowFrameTimeGraph ? GraphHeight + 4 : 0;
        var totalHeight = textHeight + graphHeight + Padding.Vertical;
        var totalWidth = Math.Min(220, availableSize.Width);

        return new Size(totalWidth, totalHeight);
    }

    protected override void ArrangeOverride(Rect contentRect)
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
            HudBackground.R, HudBackground.G, HudBackground.B, HudBackground.A
        );

        var y = contentRect.Y + 4;
        var x = contentRect.X + 6;

        PaintFpsLine(renderer, x, ref y);

        if (ShowDrawCallInfo)
        {
            PaintDrawCallLine(renderer, x, ref y);
        }

        if (ShowMemoryInfo)
        {
            PaintMemoryLine(renderer, x, ref y);
        }

        if (ShowFrameTimeGraph)
        {
            PaintFrameTimeGraph(renderer, contentRect.X + 4, y, contentRect.Width - 8, GraphHeight);
        }
    }

    private void PaintFpsLine(IWidgetRenderer renderer, float x, ref float y)
    {
        var fpsColor = GetFrameTimeColor(_currentFrameTimeMs);
        var fpsText = $"FPS: {_currentFps:F0} ({_currentFrameTimeMs:F1}ms)";
        renderer.DrawText(fpsText, x, y, FontSize, fpsColor.R, fpsColor.G, fpsColor.B);
        y += FontSize * 1.4f + 2;
    }

    private void PaintDrawCallLine(IWidgetRenderer renderer, float x, ref float y)
    {
        var drawCallText = $"Draw Calls: {_drawCallCount}";
        renderer.DrawText(drawCallText, x, y, FontSize, NormalTextColor.R, NormalTextColor.G, NormalTextColor.B);
        y += FontSize * 1.4f + 2;
    }

    private void PaintMemoryLine(IWidgetRenderer renderer, float x, ref float y)
    {
        var memoryMb = _totalMemoryBytes / (1024.0 * 1024.0);
        var memoryText = $"Memory: {memoryMb:F1} MB";
        var memoryColor = memoryMb > 512 ? WarningTextColor : NormalTextColor;
        renderer.DrawText(memoryText, x, y, FontSize, memoryColor.R, memoryColor.G, memoryColor.B);
        y += FontSize * 1.4f + 2;
    }

    private void PaintFrameTimeGraph(IWidgetRenderer renderer, float x, float y, float width, float height)
    {
        renderer.DrawRect(
            x, y, width, height,
            0.05f, 0.05f, 0.05f, 0.5f
        );

        var maxMs = Math.Max(CriticalFrameTimeMs * 1.2f, MaxFrameTimeMs * 1.1f);

        PaintThresholdLine(renderer, x, y, width, height, maxMs, WarningFrameTimeMs, GraphWarningColor);
        PaintThresholdLine(renderer, x, y, width, height, maxMs, CriticalFrameTimeMs, GraphCriticalColor);

        if (_frameCount < 2)
        {
            return;
        }

        var count = Math.Min(_frameCount, MaxFrameTimeSamples);
        var step = width / Math.Max(1, count - 1);

        for (var i = 1; i < count; i++)
        {
            var prevIdx = (_frameTimeIndex + i - 1) % MaxFrameTimeSamples;
            var currIdx = (_frameTimeIndex + i) % MaxFrameTimeSamples;

            var prevVal = _frameTimeSamples[prevIdx] / maxMs;
            var currVal = _frameTimeSamples[currIdx] / maxMs;

            prevVal = Math.Clamp(prevVal, 0, 1);
            currVal = Math.Clamp(currVal, 0, 1);

            var x1 = x + (i - 1) * step;
            var y1 = y + height - prevVal * height;
            var x2 = x + i * step;
            var y2 = y + height - currVal * height;

            var lineColor = GetFrameTimeColor(_frameTimeSamples[currIdx]);

            renderer.DrawLine(
                x1, y1, x2, y2,
                lineColor.R, lineColor.G, lineColor.B,
                0.8f, 1.0f
            );
        }
    }

    private void PaintThresholdLine(IWidgetRenderer renderer, float x, float y, float width, float height, float maxMs, float thresholdMs, Color color)
    {
        var ratio = thresholdMs / maxMs;
        var lineY = y + height - ratio * height;

        renderer.DrawLine(
            x, lineY, x + width, lineY,
            color.R, color.G, color.B,
            color.A, 1.0f
        );
    }

    private Color GetFrameTimeColor(float frameTimeMs)
    {
        if (frameTimeMs >= CriticalFrameTimeMs)
        {
            return CriticalTextColor;
        }

        if (frameTimeMs >= WarningFrameTimeMs)
        {
            return WarningTextColor;
        }

        return NormalTextColor;
    }

    #endregion

    #region 内存统计

    private void UpdateMemoryStats()
    {
        var currentMemory = GC.GetTotalMemory(false);

        if (currentMemory != _lastGcMemory || _totalMemoryBytes == 0)
        {
            _totalMemoryBytes = currentMemory;
            _lastGcMemory = currentMemory;
        }
    }

    #endregion
}
