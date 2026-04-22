using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Control;

public sealed class TextBox : WidgetElement
{
    #region 属性

    public string Text { get; set; } = "";

    public string Placeholder { get; set; } = "";

    public float FontSize { get; set; } = 12;

    public float CursorPosition { get; set; }

    public new bool IsFocused { get; set; }

    public bool IsReadOnly { get; set; }

    public new Color BorderColor { get; set; } = new(0.30f, 0.30f, 0.34f, 1.0f);

    public Color FocusedBorderColor { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    public Color PlaceholderColor { get; set; } = new(0.45f, 0.45f, 0.50f, 1.0f);

    public Color CursorColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    public Color SelectionColor { get; set; } = new(0.35f, 0.55f, 0.90f, 0.30f);

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        var textWidth = string.IsNullOrEmpty(Text) ? 0 : Text.Length * FontSize * 0.6f;
        var height = FontSize * 1.4f + Padding.Vertical;

        return new Size(
            Math.Min(Math.Max(textWidth + 16, 80), availableSize.Width),
            Math.Min(height, availableSize.Height)
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        var borderColor = IsFocused ? FocusedBorderColor : BorderColor;

        if (Border.Top > 0)
        {
            renderer.DrawRect(
                LayoutRect.Deflate(Margin).X, LayoutRect.Deflate(Margin).Y,
                LayoutRect.Deflate(Margin).Width, Border.Top,
                borderColor.R, borderColor.G, borderColor.B, borderColor.A
            );
        }

        if (Background.A > 0)
        {
            renderer.DrawRect(
                contentRect.X, contentRect.Y,
                contentRect.Width, contentRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        var displayText = Text;
        var textColor = Foreground;

        if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
        {
            displayText = Placeholder;
            textColor = PlaceholderColor;
        }

        if (!string.IsNullOrEmpty(displayText))
        {
            renderer.DrawText(
                displayText,
                contentRect.X + 4,
                contentRect.Y + 2,
                FontSize,
                textColor.R, textColor.G, textColor.B
            );
        }

        if (IsFocused && !IsReadOnly)
        {
            var cursorX = contentRect.X + 4 + CursorPosition * FontSize * 0.6f;

            renderer.DrawLine(
                cursorX, contentRect.Y + 2,
                cursorX, contentRect.Bottom - 2,
                CursorColor.R, CursorColor.G, CursorColor.B
            );
        }
    }

    #endregion

    #region 公开方法

    public void InsertText(string text)
    {
        if (IsReadOnly)
        {
            return;
        }

        Text = Text.Insert((int)Math.Clamp(CursorPosition, 0, Text.Length), text);
        CursorPosition += text.Length;
    }

    public void DeleteBackward()
    {
        if (IsReadOnly || CursorPosition <= 0)
        {
            return;
        }

        var pos = (int)CursorPosition - 1;
        Text = Text.Remove(pos, 1);
        CursorPosition = pos;
    }

    public void DeleteForward()
    {
        if (IsReadOnly || CursorPosition >= Text.Length)
        {
            return;
        }

        Text = Text.Remove((int)CursorPosition, 1);
    }

    public void MoveCursorToStart()
    {
        CursorPosition = 0;
    }

    public void MoveCursorToEnd()
    {
        CursorPosition = Text.Length;
    }

    #endregion
}
