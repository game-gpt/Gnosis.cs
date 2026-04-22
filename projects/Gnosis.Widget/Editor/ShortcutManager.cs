using Gnosis.Widget.Element;

namespace Gnosis.Widget.Editor;

public sealed class ShortcutManager
{
    #region 内部类型

    private sealed class ShortcutEntry
    {
        public Key Key { get; }
        public KeyModifiers Modifiers { get; }
        public string Name { get; }
        public Action Action { get; }
        public string DisplayString { get; }

        public ShortcutEntry(Key key, KeyModifiers modifiers, string name, Action action, string displayString)
        {
            Key = key;
            Modifiers = modifiers;
            Name = name;
            Action = action;
            DisplayString = displayString;
        }
    }

    #endregion

    #region 字段

    private readonly List<ShortcutEntry> _entries = [];

    #endregion

    #region 公开方法

    public void Register(string shortcut, string name, Action action)
    {
        var (key, modifiers) = ParseShortcut(shortcut);
        _entries.Add(new ShortcutEntry(key, modifiers, name, action, shortcut));
    }

    public void Unregister(string name)
    {
        _entries.RemoveAll(e => e.Name == name);
    }

    public bool HandleKeyEvent(Key key, KeyModifiers modifiers)
    {
        foreach (var entry in _entries)
        {
            if (entry.Key == key && entry.Modifiers == modifiers)
            {
                entry.Action();
                return true;
            }
        }

        return false;
    }

    public string? GetShortcutDisplay(string name)
    {
        foreach (var entry in _entries)
        {
            if (entry.Name == name)
            {
                return entry.DisplayString;
            }
        }

        return null;
    }

    public IReadOnlyList<(string Name, string Shortcut)> GetAllShortcuts()
    {
        return _entries.Select(e => (e.Name, e.DisplayString)).ToList();
    }

    #endregion

    #region 解析方法

    private static (Key Key, KeyModifiers Modifiers) ParseShortcut(string shortcut)
    {
        var modifiers = KeyModifiers.None;
        var key = Key.None;

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            switch (trimmed.ToLowerInvariant())
            {
                case "ctrl":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;
                default:
                    key = ParseKey(trimmed);
                    break;
            }
        }

        return (key, modifiers);
    }

    private static Key ParseKey(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "A" => Key.A, "B" => Key.B, "C" => Key.C, "D" => Key.D,
            "E" => Key.E, "F" => Key.F, "G" => Key.G, "H" => Key.H,
            "I" => Key.I, "J" => Key.J, "K" => Key.K, "L" => Key.L,
            "M" => Key.M, "N" => Key.N, "O" => Key.O, "P" => Key.P,
            "Q" => Key.Q, "R" => Key.R, "S" => Key.S, "T" => Key.T,
            "U" => Key.U, "V" => Key.V, "W" => Key.W, "X" => Key.X,
            "Y" => Key.Y, "Z" => Key.Z,
            "0" => Key.D0, "1" => Key.D1, "2" => Key.D2, "3" => Key.D3,
            "4" => Key.D4, "5" => Key.D5, "6" => Key.D6, "7" => Key.D7,
            "8" => Key.D8, "9" => Key.D9,
            "F1" => Key.F1, "F2" => Key.F2, "F3" => Key.F3, "F4" => Key.F4,
            "F5" => Key.F5, "F6" => Key.F6, "F7" => Key.F7, "F8" => Key.F8,
            "F9" => Key.F9, "F10" => Key.F10, "F11" => Key.F11, "F12" => Key.F12,
            "SPACE" => Key.Space, "ENTER" => Key.Enter, "ESCAPE" or "ESC" => Key.Escape,
            "TAB" => Key.Tab, "DELETE" or "DEL" => Key.Delete, "BACKSPACE" => Key.Back,
            "LEFT" => Key.Left, "RIGHT" => Key.Right, "UP" => Key.Up, "DOWN" => Key.Down,
            "HOME" => Key.Home, "END" => Key.End,
            "PAGEUP" => Key.PageUp, "PAGEDOWN" => Key.PageDown,
            _ => Key.None
        };
    }

    #endregion
}
