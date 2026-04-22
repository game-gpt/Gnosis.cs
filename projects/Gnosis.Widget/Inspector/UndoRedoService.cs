namespace Gnosis.Widget.Inspector;

public sealed class UndoRedoService
{
    #region 内部类型

    private sealed class UndoAction
    {
        public string Name { get; }
        public Action Undo { get; }
        public Action Redo { get; }

        public UndoAction(string name, Action undo, Action redo)
        {
            Name = name;
            Undo = undo;
            Redo = redo;
        }
    }

    #endregion

    #region 字段

    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();
    private int _maxStackSize = 100;

    #endregion

    #region 属性

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;

    public int RedoCount => _redoStack.Count;

    #endregion

    #region 事件

    public event Action? StateChanged;

    #endregion

    #region 公开方法

    public void Execute(string name, Action doAction, Action undoAction)
    {
        doAction();
        Record(name, undoAction, doAction);
    }

    public void Record(string name, Action undoAction, Action redoAction)
    {
        _undoStack.Push(new UndoAction(name, undoAction, redoAction));
        _redoStack.Clear();

        TrimStackIfNeeded(_undoStack);
        StateChanged?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        StateChanged?.Invoke();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);

        StateChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    public string? GetUndoName()
    {
        return _undoStack.Count > 0 ? _undoStack.Peek().Name : null;
    }

    public string? GetRedoName()
    {
        return _redoStack.Count > 0 ? _redoStack.Peek().Name : null;
    }

    #endregion

    #region 私有方法

    private void TrimStackIfNeeded(Stack<UndoAction> stack)
    {
        while (stack.Count > _maxStackSize)
        {
            var items = stack.ToArray();
            stack.Clear();
            for (var i = items.Length - 1; i >= 1; i--)
            {
                stack.Push(items[i]);
            }
        }
    }

    #endregion
}
