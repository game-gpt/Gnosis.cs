namespace Gnosis.Widget.Scene;

public sealed class SelectionService
{
    #region 字段

    private readonly List<object> _selectedObjects = [];
    private object? _primarySelection;

    #endregion

    #region 属性

    public IReadOnlyList<object> SelectedObjects => _selectedObjects;

    public object? PrimarySelection => _primarySelection;

    public int SelectionCount => _selectedObjects.Count;

    #endregion

    #region 事件

    public event Action? SelectionChanged;

    #endregion

    #region 公开方法

    public void Select(object obj)
    {
        _selectedObjects.Clear();
        _selectedObjects.Add(obj);
        _primarySelection = obj;
        SelectionChanged?.Invoke();
    }

    public void AddToSelection(object obj)
    {
        if (_selectedObjects.Contains(obj))
        {
            return;
        }

        _selectedObjects.Add(obj);
        _primarySelection = obj;
        SelectionChanged?.Invoke();
    }

    public void RemoveFromSelection(object obj)
    {
        _selectedObjects.Remove(obj);

        if (_primarySelection == obj)
        {
            _primarySelection = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        }

        SelectionChanged?.Invoke();
    }

    public void ToggleSelection(object obj)
    {
        if (_selectedObjects.Contains(obj))
        {
            RemoveFromSelection(obj);
        }
        else
        {
            AddToSelection(obj);
        }
    }

    public void ClearSelection()
    {
        _selectedObjects.Clear();
        _primarySelection = null;
        SelectionChanged?.Invoke();
    }

    public bool IsSelected(object obj)
    {
        return _selectedObjects.Contains(obj);
    }

    public void SetSelection(IEnumerable<object> objects)
    {
        _selectedObjects.Clear();
        _selectedObjects.AddRange(objects);
        _primarySelection = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        SelectionChanged?.Invoke();
    }

    #endregion
}
