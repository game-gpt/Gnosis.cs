using Gnosis.Widget.Element;

namespace Gnosis.Widget.Window;

public sealed class WindowManager
{
    private readonly List<WindowPane> _windows = [];
    private WindowPane? _selectedWindow;

    public IReadOnlyList<WindowPane> Windows => _windows;

    public WindowPane? SelectedWindow => _selectedWindow;

    public event Action<WindowPane>? WindowAdded;
    public event Action<WindowPane>? WindowRemoved;
    public event Action<WindowPane>? WindowSelected;

    public WindowPane CreateWindow(string title, WidgetElement content)
    {
        var pane = new WindowPane
        {
            Title = title,
            Width = 300,
            Height = 400
        };

        pane.AddChild(content);
        _windows.Add(pane);
        SelectWindow(pane);
        WindowAdded?.Invoke(pane);
        return pane;
    }

    public void RemoveWindow(WindowPane pane)
    {
        _windows.Remove(pane);

        if (_selectedWindow == pane)
        {
            _selectedWindow = _windows.Count > 0 ? _windows[^1] : null;
            if (_selectedWindow != null)
            {
                _selectedWindow.IsSelected = true;
            }
        }

        WindowRemoved?.Invoke(pane);
    }

    public void SelectWindow(WindowPane pane)
    {
        if (_selectedWindow != null)
        {
            _selectedWindow.IsSelected = false;
        }

        _selectedWindow = pane;
        pane.IsSelected = true;
        WindowSelected?.Invoke(pane);
    }

    public void CloseWindow(WindowPane pane)
    {
        if (!pane.CanClose)
        {
            return;
        }

        RemoveWindow(pane);
    }

    public void MinimizeWindow(WindowPane pane)
    {
        pane.State = WindowState.Minimized;
    }

    public void MaximizeWindow(WindowPane pane)
    {
        pane.State = WindowState.Maximized;
    }

    public void RestoreWindow(WindowPane pane)
    {
        pane.State = WindowState.Floating;
    }
}
