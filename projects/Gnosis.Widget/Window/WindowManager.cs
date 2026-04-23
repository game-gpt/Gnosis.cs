using Gnosis.Widget.Element;

namespace Gnosis.Widget.Window;

public sealed class WindowManager
{
    #region 字段

    private readonly List<WindowPane> _windows = [];
    private WindowPane? _selectedWindow;
    private WindowPane? _draggingWindow;
    private WindowPane? _resizingWindow;
    private WindowPane? _modalOwner;
    private WindowPane? _modalDialog;

    #endregion

    #region 属性

    public IReadOnlyList<WindowPane> Windows => _windows;

    public WindowPane? SelectedWindow => _selectedWindow;

    public WindowPane? ModalDialog => _modalDialog;

    public bool HasModalDialog => _modalDialog != null;

    #endregion

    #region 事件

    public event Action<WindowPane>? WindowAdded;
    public event Action<WindowPane>? WindowRemoved;
    public event Action<WindowPane>? WindowSelected;
    public event Action<WindowPane>? WindowMoved;
    public event Action<WindowPane>? WindowResized;
    public event Action<WindowPane>? WindowStateChanged;
    public event Action<WindowPane>? ModalDialogOpened;
    public event Action<WindowPane>? ModalDialogClosed;

    #endregion

    #region 窗口管理

    public WindowPane CreateWindow(string title, WidgetElement content, float x = 100, float y = 100, float width = 300, float height = 400)
    {
        var pane = new WindowPane
        {
            Title = title,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            MinWidth = 100,
            MinHeight = 80
        };

        pane.AddChild(content);
        _windows.Add(pane);
        SelectWindow(pane);
        WindowAdded?.Invoke(pane);
        return pane;
    }

    public void RemoveWindow(WindowPane pane)
    {
        if (pane == _modalDialog)
        {
            CloseModalDialog();
            return;
        }

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
        if (HasModalDialog && pane != _modalDialog)
        {
            return;
        }

        if (_selectedWindow != null)
        {
            _selectedWindow.IsSelected = false;
        }

        _selectedWindow = pane;
        pane.IsSelected = true;
        BringToFront(pane);
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
        WindowStateChanged?.Invoke(pane);
    }

    public void MaximizeWindow(WindowPane pane)
    {
        pane.State = WindowState.Maximized;
        WindowStateChanged?.Invoke(pane);
    }

    public void RestoreWindow(WindowPane pane)
    {
        pane.State = WindowState.Floating;
        WindowStateChanged?.Invoke(pane);
    }

    public void ToggleMaximize(WindowPane pane)
    {
        if (pane.State == WindowState.Maximized)
        {
            RestoreWindow(pane);
        }
        else
        {
            MaximizeWindow(pane);
        }
    }

    #endregion

    #region Z-Order 管理

    public void BringToFront(WindowPane pane)
    {
        var index = _windows.IndexOf(pane);
        if (index < 0 || index == _windows.Count - 1)
        {
            return;
        }

        _windows.RemoveAt(index);
        _windows.Add(pane);
    }

    public void SendToBack(WindowPane pane)
    {
        var index = _windows.IndexOf(pane);
        if (index <= 0)
        {
            return;
        }

        _windows.RemoveAt(index);
        _windows.Insert(0, pane);
    }

    public int GetZOrder(WindowPane pane)
    {
        return _windows.IndexOf(pane);
    }

    #endregion

    #region 拖拽管理

    public bool BeginDrag(WindowPane pane, float mouseX, float mouseY)
    {
        if (HasModalDialog && pane != _modalDialog)
        {
            return false;
        }

        if (pane.State != WindowState.Floating)
        {
            return false;
        }

        var contentRect = pane.LayoutRect.Deflate(pane.Margin);

        _draggingWindow = pane;
        pane.IsDragging = true;
        pane.DragOffsetX = mouseX - contentRect.X;
        pane.DragOffsetY = mouseY - contentRect.Y;

        SelectWindow(pane);
        return true;
    }

    public void Drag(float mouseX, float mouseY)
    {
        if (_draggingWindow == null)
        {
            return;
        }

        _draggingWindow.X = mouseX - _draggingWindow.DragOffsetX;
        _draggingWindow.Y = mouseY - _draggingWindow.DragOffsetY;

        WindowMoved?.Invoke(_draggingWindow);
    }

    public void EndDrag()
    {
        if (_draggingWindow == null)
        {
            return;
        }

        _draggingWindow.IsDragging = false;
        _draggingWindow = null;
    }

    public bool BeginResize(WindowPane pane, float mouseX, float mouseY, ResizeEdge edge)
    {
        if (HasModalDialog && pane != _modalDialog)
        {
            return false;
        }

        if (pane.State != WindowState.Floating || !pane.CanResize)
        {
            return false;
        }

        _resizingWindow = pane;
        pane.IsResizing = true;
        pane.ResizeEdge = edge;
        pane.ResizeStartWidth = pane.Width ?? 300;
        pane.ResizeStartHeight = pane.Height ?? 400;
        pane.ResizeStartX = pane.X;
        pane.ResizeStartY = pane.Y;

        SelectWindow(pane);
        return true;
    }

    public void Resize(float mouseX, float mouseY)
    {
        if (_resizingWindow == null)
        {
            return;
        }

        var pane = _resizingWindow;
        var edge = pane.ResizeEdge;
        var dx = mouseX - (pane.X + pane.DragOffsetX);
        var dy = mouseY - (pane.Y + pane.DragOffsetY);

        var newWidth = pane.ResizeStartWidth;
        var newHeight = pane.ResizeStartHeight;
        var newX = pane.ResizeStartX;
        var newY = pane.ResizeStartY;

        if (edge.HasFlag(ResizeEdge.Right))
        {
            newWidth = Math.Max(pane.MinWidth, pane.ResizeStartWidth + dx);
        }

        if (edge.HasFlag(ResizeEdge.Left))
        {
            var widthDelta = pane.ResizeStartWidth - newWidth;
            newWidth = Math.Max(pane.MinWidth, pane.ResizeStartWidth - dx);
            newX = pane.ResizeStartX + (pane.ResizeStartWidth - newWidth);
        }

        if (edge.HasFlag(ResizeEdge.Bottom))
        {
            newHeight = Math.Max(pane.MinHeight, pane.ResizeStartHeight + dy);
        }

        if (edge.HasFlag(ResizeEdge.Top))
        {
            newHeight = Math.Max(pane.MinHeight, pane.ResizeStartHeight - dy);
            newY = pane.ResizeStartY + (pane.ResizeStartHeight - newHeight);
        }

        pane.Width = newWidth;
        pane.Height = newHeight;
        pane.X = newX;
        pane.Y = newY;

        WindowResized?.Invoke(pane);
    }

    public void EndResize()
    {
        if (_resizingWindow == null)
        {
            return;
        }

        _resizingWindow.IsResizing = false;
        _resizingWindow = null;
    }

    #endregion

    #region 模态对话框

    public WindowPane ShowModalDialog(string title, WidgetElement content, WindowPane owner, float width = 400, float height = 300)
    {
        if (_modalDialog != null)
        {
            CloseModalDialog();
        }

        _modalOwner = owner;

        var dialog = new WindowPane
        {
            Title = title,
            X = owner.X + ((owner.Width ?? 300) - width) / 2,
            Y = owner.Y + ((owner.Height ?? 400) - height) / 2,
            Width = width,
            Height = height,
            CanDock = false,
            CanResize = false
        };

        dialog.AddChild(content);
        _windows.Add(dialog);
        _modalDialog = dialog;

        SelectWindow(dialog);
        ModalDialogOpened?.Invoke(dialog);
        return dialog;
    }

    public void CloseModalDialog()
    {
        if (_modalDialog == null)
        {
            return;
        }

        var dialog = _modalDialog;
        _modalDialog = null;

        _windows.Remove(dialog);

        if (_selectedWindow == dialog)
        {
            _selectedWindow = _modalOwner;
            if (_modalOwner != null)
            {
                _modalOwner.IsSelected = true;
            }
        }

        _modalOwner = null;
        ModalDialogClosed?.Invoke(dialog);
    }

    #endregion

    #region 命中测试

    public WindowPane? HitTestWindow(float x, float y)
    {
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            var pane = _windows[i];

            if (pane.State == WindowState.Minimized)
            {
                continue;
            }

            if (pane.HitTest(x, y))
            {
                return pane;
            }
        }

        return null;
    }

    #endregion

    #region 输入处理

    public bool HandleMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
        {
            return false;
        }

        var hitPane = HitTestWindow(x, y);

        if (hitPane == null)
        {
            return false;
        }

        if (HasModalDialog && hitPane != _modalDialog)
        {
            return true;
        }

        SelectWindow(hitPane);

        if (hitPane.HitTestCloseButton(x, y))
        {
            CloseWindow(hitPane);
            return true;
        }

        if (hitPane.HitTestMaximizeButton(x, y))
        {
            ToggleMaximize(hitPane);
            return true;
        }

        if (hitPane.HitTestMinimizeButton(x, y))
        {
            MinimizeWindow(hitPane);
            return true;
        }

        var resizeEdge = hitPane.HitTestResizeEdge(x, y);
        if (resizeEdge != ResizeEdge.None)
        {
            BeginResize(hitPane, x, y, resizeEdge);
            return true;
        }

        if (hitPane.HitTestTitleBar(x, y))
        {
            BeginDrag(hitPane, x, y);
            return true;
        }

        return false;
    }

    public bool HandleMouseMove(float x, float y)
    {
        if (_draggingWindow != null)
        {
            Drag(x, y);
            return true;
        }

        if (_resizingWindow != null)
        {
            Resize(x, y);
            return true;
        }

        return false;
    }

    public bool HandleMouseUp(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
        {
            return false;
        }

        if (_draggingWindow != null)
        {
            EndDrag();
            return true;
        }

        if (_resizingWindow != null)
        {
            EndResize();
            return true;
        }

        return false;
    }

    #endregion
}
