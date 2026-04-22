namespace Gnosis.Widget.Element;

public abstract class WidgetEventArgs
{
    public bool Handled { get; set; } = false;
}

public sealed class MouseEventArgs : WidgetEventArgs
{
    public float X { get; }
    public float Y { get; }
    public MouseButton Button { get; }
    public int ClickCount { get; }
    public float DeltaX { get; }
    public float DeltaY { get; }

    public MouseEventArgs(float x, float y, MouseButton button = MouseButton.None, int clickCount = 1, float deltaX = 0, float deltaY = 0)
    {
        X = x;
        Y = y;
        Button = button;
        ClickCount = clickCount;
        DeltaX = deltaX;
        DeltaY = deltaY;
    }
}

public sealed class KeyEventArgs : WidgetEventArgs
{
    public Key Key { get; }
    public KeyModifiers Modifiers { get; }
    public bool IsRepeat { get; }

    public KeyEventArgs(Key key, KeyModifiers modifiers = KeyModifiers.None, bool isRepeat = false)
    {
        Key = key;
        Modifiers = modifiers;
        IsRepeat = isRepeat;
    }
}

public sealed class TextInputEventArgs : WidgetEventArgs
{
    public string Text { get; }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}

public sealed class FocusEventArgs : WidgetEventArgs
{
    public WidgetElement? OldFocus { get; }
    public WidgetElement? NewFocus { get; }

    public FocusEventArgs(WidgetElement? oldFocus, WidgetElement? newFocus)
    {
        OldFocus = oldFocus;
        NewFocus = newFocus;
    }
}

public sealed class WheelEventArgs : WidgetEventArgs
{
    public float X { get; }
    public float Y { get; }
    public float Delta { get; }

    public WheelEventArgs(float x, float y, float delta)
    {
        X = x;
        Y = y;
        Delta = delta;
    }
}
