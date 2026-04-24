using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Timeline;

public sealed class TimelineTrack
{
    public string Name { get; }
    public Color Color { get; }
    public List<TimelineKeyframe> Keyframes { get; } = [];

    public TimelineTrack(string name, Color? color = null)
    {
        Name = name;
        Color = color ?? new Color(0.4f, 0.6f, 0.9f, 1.0f);
    }
}

public sealed class TimelineKeyframe
{
    public float Time { get; set; }
    public string Label { get; set; }
    public Dictionary<string, object> Data { get; } = new();

    public TimelineKeyframe(float time, string label = "")
    {
        Time = time;
        Label = label;
    }
}

public sealed class TimelineWidget : ContainerElement
{
    #region 常量

    private const float TrackHeight = 28.0f;
    private const float HeaderWidth = 120.0f;
    private const float PixelsPerSecond = 80.0f;
    private const float MinTime = 0.0f;
    private const float MaxTime = 300.0f;

    #endregion

    #region 状态

    private float _scrollX;
    private float _scrollY;
    private float _playheadTime;
    private bool _isPlaying;
    private bool _isDraggingPlayhead;
    private bool _isDraggingScroll;
    private float _dragStartX;
    private float _dragStartScrollX;
    private int? _selectedKeyframeTrackIndex;
    private int? _selectedKeyframeIndex;

    #endregion

    #region 数据

    private readonly List<TimelineTrack> _tracks = [];

    #endregion

    #region 事件

    public event Action<float>? PlayheadMoved;
    public event Action<int, int>? KeyframeSelected;

    #endregion

    #region 属性

    public float PlayheadTime => _playheadTime;
    public bool IsPlaying => _isPlaying;
    public IReadOnlyList<TimelineTrack> Tracks => _tracks;

    #endregion

    #region 构造函数

    public TimelineWidget()
    {
        IsFocusable = true;
        Background = new Color(0.18f, 0.18f, 0.22f, 1.0f);
    }

    #endregion

    #region 公共方法

    public TimelineTrack AddTrack(string name, Color? color = null)
    {
        var track = new TimelineTrack(name, color);
        _tracks.Add(track);
        InvalidateMeasure();
        return track;
    }

    public void RemoveTrack(int index)
    {
        if (index >= 0 && index < _tracks.Count)
        {
            _tracks.RemoveAt(index);
            InvalidateMeasure();
        }
    }

    public void SetPlayheadTime(float time)
    {
        _playheadTime = Math.Clamp(time, MinTime, MaxTime);
        PlayheadMoved?.Invoke(_playheadTime);
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void Tick(float deltaTime)
    {
        if (_isPlaying)
        {
            _playheadTime += deltaTime;
            if (_playheadTime > MaxTime)
            {
                _playheadTime = MinTime;
            }

            PlayheadMoved?.Invoke(_playheadTime);
        }
    }

    public void ClearTracks()
    {
        _tracks.Clear();
        _selectedKeyframeTrackIndex = null;
        _selectedKeyframeIndex = null;
        InvalidateMeasure();
    }

    #endregion

    #region 布局

    protected override Size MeasureChildren(Size availableSize)
    {
        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        foreach (var child in Children)
        {
            child.Arrange(contentRect);
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        renderer.PushClip(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height);

        PaintHeader(renderer, contentRect);
        PaintTimeRuler(renderer, contentRect);
        PaintTracks(renderer, contentRect);
        PaintPlayhead(renderer, contentRect);

        renderer.PopClip();
    }

    private void PaintHeader(IWidgetRenderer renderer, Rect contentRect)
    {
        var headerBg = new Color(0.15f, 0.15f, 0.18f, 1.0f);
        renderer.DrawRect(contentRect.X, contentRect.Y, HeaderWidth, contentRect.Height,
            headerBg.R, headerBg.G, headerBg.B, headerBg.A);

        renderer.DrawLine(
            contentRect.X + HeaderWidth, contentRect.Y,
            contentRect.X + HeaderWidth, contentRect.Bottom,
            0.3f, 0.3f, 0.35f, 1.0f);

        var trackY = contentRect.Y + 24 - _scrollY;

        for (var i = 0; i < _tracks.Count; i++)
        {
            var track = _tracks[i];

            if (trackY + TrackHeight < contentRect.Y || trackY > contentRect.Bottom)
            {
                trackY += TrackHeight;
                continue;
            }

            var isSelected = i == _selectedKeyframeTrackIndex;
            var rowBg = isSelected
                ? new Color(0.25f, 0.25f, 0.3f, 1.0f)
                : new Color(0.18f, 0.18f, 0.22f, 1.0f);

            renderer.DrawRect(contentRect.X, trackY, HeaderWidth, TrackHeight,
                rowBg.R, rowBg.G, rowBg.B, rowBg.A);

            renderer.DrawRect(contentRect.X + 4, trackY + 4, 8, TrackHeight - 8,
                track.Color.R, track.Color.G, track.Color.B, track.Color.A);

            renderer.DrawText(track.Name, contentRect.X + 18, trackY + 8, 9,
                0.85f, 0.85f, 0.85f);

            trackY += TrackHeight;
        }
    }

    private void PaintTimeRuler(IWidgetRenderer renderer, Rect contentRect)
    {
        var rulerY = contentRect.Y;
        var rulerHeight = 24.0f;
        var rulerBg = new Color(0.13f, 0.13f, 0.16f, 1.0f);

        renderer.DrawRect(contentRect.X + HeaderWidth, rulerY,
            contentRect.Width - HeaderWidth, rulerHeight,
            rulerBg.R, rulerBg.G, rulerBg.B, rulerBg.A);

        var startX = contentRect.X + HeaderWidth - _scrollX;

        for (var t = 0.0f; t <= MaxTime; t += 1.0f)
        {
            var x = startX + t * PixelsPerSecond;

            if (x < contentRect.X + HeaderWidth || x > contentRect.Right)
            {
                continue;
            }

            var isMajor = Math.Abs(t % 5) < 0.01f;
            var lineColor = isMajor
                ? new Color(0.5f, 0.5f, 0.55f, 1.0f)
                : new Color(0.3f, 0.3f, 0.35f, 1.0f);
            var lineHeight = isMajor ? 12.0f : 6.0f;

            renderer.DrawLine(x, rulerY + rulerHeight - lineHeight, x, rulerY + rulerHeight,
                lineColor.R, lineColor.G, lineColor.B, lineColor.A);

            if (isMajor)
            {
                renderer.DrawText($"{t:F0}s", x + 2, rulerY + 4, 8,
                    0.6f, 0.6f, 0.65f);
            }
        }
    }

    private void PaintTracks(IWidgetRenderer renderer, Rect contentRect)
    {
        var trackAreaX = contentRect.X + HeaderWidth;
        var trackAreaY = contentRect.Y + 24;
        var trackAreaWidth = contentRect.Width - HeaderWidth;

        var trackY = trackAreaY - _scrollY;

        for (var i = 0; i < _tracks.Count; i++)
        {
            var track = _tracks[i];

            if (trackY + TrackHeight < trackAreaY || trackY > contentRect.Bottom)
            {
                trackY += TrackHeight;
                continue;
            }

            var isSelected = i == _selectedKeyframeTrackIndex;
            var rowBg = isSelected
                ? new Color(0.22f, 0.22f, 0.27f, 1.0f)
                : new Color(0.16f, 0.16f, 0.2f, 1.0f);

            renderer.DrawRect(trackAreaX, trackY, trackAreaWidth, TrackHeight,
                rowBg.R, rowBg.G, rowBg.B, rowBg.A);

            renderer.DrawLine(trackAreaX, trackY + TrackHeight, contentRect.Right, trackY + TrackHeight,
                0.2f, 0.2f, 0.25f, 1.0f);

            foreach (var kf in track.Keyframes)
            {
                var kfX = trackAreaX + kf.Time * PixelsPerSecond - _scrollX;

                if (kfX < trackAreaX || kfX > contentRect.Right)
                {
                    continue;
                }

                var kfY = trackY + TrackHeight / 2;
                var diamondSize = 5;

                renderer.DrawRect(kfX - diamondSize, kfY - diamondSize,
                    diamondSize * 2, diamondSize * 2,
                    track.Color.R, track.Color.G, track.Color.B, track.Color.A);

                if (!string.IsNullOrEmpty(kf.Label))
                {
                    renderer.DrawText(kf.Label, kfX + diamondSize + 2, kfY - 4, 7,
                        0.7f, 0.7f, 0.7f);
                }
            }

            trackY += TrackHeight;
        }
    }

    private void PaintPlayhead(IWidgetRenderer renderer, Rect contentRect)
    {
        var playheadX = contentRect.X + HeaderWidth + _playheadTime * PixelsPerSecond - _scrollX;

        if (playheadX < contentRect.X + HeaderWidth || playheadX > contentRect.Right)
        {
            return;
        }

        renderer.DrawLine(playheadX, contentRect.Y, playheadX, contentRect.Bottom,
            0.9f, 0.3f, 0.3f, 1.0f, 2.0f);

        renderer.DrawRect(playheadX - 6, contentRect.Y, 12, 16,
            0.9f, 0.3f, 0.3f, 1.0f);
    }

    #endregion

    #region 交互

    public void HandleMouseDown(float x, float y, MouseButton button)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        if (button == MouseButton.Left)
        {
            var trackAreaX = contentRect.X + HeaderWidth;

            if (x >= trackAreaX)
            {
                var time = (x - trackAreaX + _scrollX) / PixelsPerSecond;

                if (y < contentRect.Y + 24)
                {
                    _playheadTime = Math.Clamp(time, MinTime, MaxTime);
                    _isDraggingPlayhead = true;
                    PlayheadMoved?.Invoke(_playheadTime);
                    return;
                }

                var trackIndex = (int)((y - contentRect.Y - 24 + _scrollY) / TrackHeight);
                if (trackIndex >= 0 && trackIndex < _tracks.Count)
                {
                    _selectedKeyframeTrackIndex = trackIndex;
                    _selectedKeyframeIndex = null;

                    var track = _tracks[trackIndex];
                    for (var ki = 0; ki < track.Keyframes.Count; ki++)
                    {
                        var kfX = trackAreaX + track.Keyframes[ki].Time * PixelsPerSecond - _scrollX;
                        if (Math.Abs(x - kfX) < 8)
                        {
                            _selectedKeyframeIndex = ki;
                            KeyframeSelected?.Invoke(trackIndex, ki);
                            break;
                        }
                    }
                }
            }
        }
        else if (button == MouseButton.Middle || button == MouseButton.Right)
        {
            _isDraggingScroll = true;
            _dragStartX = x;
            _dragStartScrollX = _scrollX;
        }
    }

    public void HandleMouseMove(float x, float y)
    {
        if (_isDraggingPlayhead)
        {
            var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);
            var trackAreaX = contentRect.X + HeaderWidth;
            var time = (x - trackAreaX + _scrollX) / PixelsPerSecond;
            _playheadTime = Math.Clamp(time, MinTime, MaxTime);
            PlayheadMoved?.Invoke(_playheadTime);
        }

        if (_isDraggingScroll)
        {
            _scrollX = _dragStartScrollX - (x - _dragStartX);
            _scrollX = Math.Max(0, _scrollX);
        }
    }

    public void HandleMouseUp(float x, float y, MouseButton button)
    {
        _isDraggingPlayhead = false;
        _isDraggingScroll = false;
    }

    public void HandleWheel(float x, float y, float delta)
    {
        _scrollX += delta > 0 ? -40 : 40;
        _scrollX = Math.Max(0, _scrollX);
    }

    #endregion
}
