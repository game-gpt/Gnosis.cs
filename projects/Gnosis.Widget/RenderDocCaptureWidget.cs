using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget;

/// <summary>
/// RenderDoc 一键捕获控件，提供编辑器内 RenderDoc 集成
/// </summary>
public sealed class RenderDocCaptureWidget : WidgetElement
{
    #region 字段

    private bool _isAvailable;
    private bool _isCapturing;
    private string _statusText = "";
    private string? _lastCapturePath;
    private float _hoverAlpha;

    #endregion

    #region 属性

    /// <summary>
    /// 是否可用（RenderDoc 已连接）
    /// </summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            _isAvailable = value;
            _statusText = value ? "RenderDoc: Ready" : "RenderDoc: Not Connected";
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// 是否正在捕获
    /// </summary>
    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            _isCapturing = value;
            _statusText = value ? "Capturing..." : (_isAvailable ? "RenderDoc: Ready" : "RenderDoc: Not Connected");
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// 最近一次捕获文件路径
    /// </summary>
    public string? LastCapturePath
    {
        get => _lastCapturePath;
        set => _lastCapturePath = value;
    }

    /// <summary>
    /// 按钮文字
    /// </summary>
    public string ButtonText { get; set; } = "Capture Frame";

    /// <summary>
    /// 字体大小
    /// </summary>
    public float FontSize { get; set; } = 11;

    /// <summary>
    /// 按钮高度
    /// </summary>
    public float ButtonHeight { get; set; } = 24;

    /// <summary>
    /// 状态栏高度
    /// </summary>
    public float StatusBarHeight { get; set; } = 18;

    /// <summary>
    /// 正常背景色
    /// </summary>
    public Color NormalBackground { get; set; } = new(0.22f, 0.22f, 0.26f, 1.0f);

    /// <summary>
    /// Hover 背景色
    /// </summary>
    public Color HoverBackground { get; set; } = new(0.30f, 0.30f, 0.35f, 1.0f);

    /// <summary>
    /// 捕获中背景色
    /// </summary>
    public Color CapturingBackground { get; set; } = new(0.50f, 0.25f, 0.10f, 1.0f);

    /// <summary>
    /// 不可用背景色
    /// </summary>
    public Color DisabledBackground { get; set; } = new(0.15f, 0.15f, 0.17f, 1.0f);

    /// <summary>
    /// 正常文字颜色
    /// </summary>
    public Color NormalTextColor { get; set; } = new(0.85f, 0.85f, 0.88f, 1.0f);

    /// <summary>
    /// 不可用文字颜色
    /// </summary>
    public Color DisabledTextColor { get; set; } = new(0.45f, 0.45f, 0.48f, 1.0f);

    /// <summary>
    /// 状态文字颜色
    /// </summary>
    public Color StatusTextColor { get; set; } = new(0.55f, 0.55f, 0.60f, 1.0f);

    /// <summary>
    /// 捕获中状态颜色
    /// </summary>
    public Color CapturingTextColor { get; set; } = new(1.0f, 0.70f, 0.30f, 1.0f);

    /// <summary>
    /// 就绪状态颜色
    /// </summary>
    public Color ReadyTextColor { get; set; } = new(0.50f, 0.85f, 0.50f, 1.0f);

    #endregion

    #region 事件

    /// <summary>
    /// 请求触发单帧捕获
    /// </summary>
    public event EventHandler? CaptureRequested;

    /// <summary>
    /// 请求触发范围捕获开始
    /// </summary>
    public event EventHandler? RangeCaptureBeginRequested;

    /// <summary>
    /// 请求触发范围捕获结束
    /// </summary>
    public event EventHandler? RangeCaptureEndRequested;

    /// <summary>
    /// 请求打开最近捕获
    /// </summary>
    public event EventHandler? OpenCaptureRequested;

    #endregion

    #region 构造

    public RenderDocCaptureWidget()
    {
        _statusText = "RenderDoc: Not Connected";

        AddHandler<MouseEventArgs>(OnMouseEvent);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 触发单帧捕获
    /// </summary>
    public void TriggerCapture()
    {
        if (!_isAvailable || _isCapturing)
        {
            return;
        }

        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 开始范围捕获
    /// </summary>
    public void BeginRangeCapture()
    {
        if (!_isAvailable || _isCapturing)
        {
            return;
        }

        IsCapturing = true;
        RangeCaptureBeginRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 结束范围捕获
    /// </summary>
    public void EndRangeCapture()
    {
        if (!_isCapturing)
        {
            return;
        }

        RangeCaptureEndRequested?.Invoke(this, EventArgs.Empty);
        IsCapturing = false;
    }

    /// <summary>
    /// 打开最近一次捕获
    /// </summary>
    public void OpenLastCapture()
    {
        if (!_isAvailable)
        {
            return;
        }

        OpenCaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Min(200, availableSize.Width);
        var height = ButtonHeight + StatusBarHeight + Padding.Vertical;

        return new Size(width, height);
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        PaintCaptureButton(renderer, contentRect);
        PaintStatusBar(renderer, contentRect);
    }

    private void PaintCaptureButton(IWidgetRenderer renderer, Rect contentRect)
    {
        var buttonRect = new Rect(
            contentRect.X,
            contentRect.Y,
            contentRect.Width,
            ButtonHeight
        );

        var bgColor = GetButtonBackgroundColor();

        renderer.DrawRect(
            buttonRect.X, buttonRect.Y,
            buttonRect.Width, buttonRect.Height,
            bgColor.R, bgColor.G, bgColor.B, bgColor.A
        );

        var textColor = _isAvailable ? NormalTextColor : DisabledTextColor;

        if (_isCapturing)
        {
            textColor = CapturingTextColor;
        }

        var text = _isCapturing ? "Stop Capture" : ButtonText;
        var textX = buttonRect.X + (buttonRect.Width - text.Length * FontSize * 0.6f) / 2;
        var textY = buttonRect.Y + (buttonRect.Height - FontSize) / 2;

        renderer.DrawText(text, textX, textY, FontSize, textColor.R, textColor.G, textColor.B);
    }

    private void PaintStatusBar(IWidgetRenderer renderer, Rect contentRect)
    {
        var statusRect = new Rect(
            contentRect.X,
            contentRect.Y + ButtonHeight + 2,
            contentRect.Width,
            StatusBarHeight
        );

        var statusColor = _isCapturing ? CapturingTextColor :
            _isAvailable ? ReadyTextColor : StatusTextColor;

        renderer.DrawText(_statusText, statusRect.X + 4, statusRect.Y + 2, FontSize * 0.85f, statusColor.R, statusColor.G, statusColor.B);

        if (_isAvailable && !string.IsNullOrEmpty(_lastCapturePath))
        {
            var openText = "[Open]";
            var openX = statusRect.Right - openText.Length * FontSize * 0.85f * 0.6f - 4;
            var openColor = new Color(0.5f, 0.7f, 1.0f, 1.0f);

            renderer.DrawText(openText, openX, statusRect.Y + 2, FontSize * 0.85f, openColor.R, openColor.G, openColor.B);
        }
    }

    private Color GetButtonBackgroundColor()
    {
        if (_isCapturing)
        {
            return CapturingBackground;
        }

        if (!_isAvailable)
        {
            return DisabledBackground;
        }

        return _hoverAlpha > 0.5f ? HoverBackground : NormalBackground;
    }

    #endregion

    #region 事件处理

    private void OnMouseEvent(WidgetElement sender, MouseEventArgs e)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        var buttonRect = new Rect(
            contentRect.X,
            contentRect.Y,
            contentRect.Width,
            ButtonHeight
        );

        var isOverButton = HitTestRect(e.X, e.Y, buttonRect);

        if (isOverButton && _hoverAlpha < 1.0f)
        {
            _hoverAlpha = 1.0f;
            InvalidateArrange();
        }
        else if (!isOverButton && _hoverAlpha > 0.0f)
        {
            _hoverAlpha = 0.0f;
            InvalidateArrange();
        }

        if (e.Button != MouseButton.Left)
        {
            return;
        }

        if (isOverButton)
        {
            if (_isCapturing)
            {
                EndRangeCapture();
            }
            else
            {
                TriggerCapture();
            }

            e.Handled = true;
            return;
        }

        var statusRect = new Rect(
            contentRect.X,
            contentRect.Y + ButtonHeight + 2,
            contentRect.Width,
            StatusBarHeight
        );

        if (HitTestRect(e.X, e.Y, statusRect) && _isAvailable)
        {
            OpenLastCapture();
            e.Handled = true;
        }
    }

    private static bool HitTestRect(float x, float y, Rect rect)
    {
        return x >= rect.X && x <= rect.X + rect.Width &&
               y >= rect.Y && y <= rect.Y + rect.Height;
    }

    #endregion
}
