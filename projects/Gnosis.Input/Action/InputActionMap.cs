namespace Gnosis.Input.Action;

public sealed class InputActionMap : IInputActionMap
{
    #region 字段

    private readonly List<IInputAction> _actionList = new();
    private readonly Dictionary<string, IInputAction> _actionsByName = new();

    #endregion

    #region 属性

    public string Name { get; }

    public bool IsEnabled { get; set; }

    public IReadOnlyList<IInputAction> Actions => _actionList;

    #endregion

    #region 构造函数

    public InputActionMap(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsEnabled = true;
    }

    #endregion

    #region IInputActionMap 实现

    public IInputAction GetAction(string name)
    {
        if (_actionsByName.TryGetValue(name, out var action))
        {
            return action;
        }

        throw new KeyNotFoundException($"输入动作未找到：{name}");
    }

    public void AddAction(IInputAction action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _actionList.Add(action);
        _actionsByName[action.Name] = action;
    }

    public void RemoveAction(string name)
    {
        if (_actionsByName.TryGetValue(name, out var action))
        {
            _actionList.Remove(action);
            _actionsByName.Remove(name);
        }
    }

    public void Enable()
    {
        IsEnabled = true;

        foreach (var action in _actionList)
        {
            action.IsEnabled = true;
        }
    }

    public void Disable()
    {
        IsEnabled = false;

        foreach (var action in _actionList)
        {
            action.IsEnabled = false;
        }
    }

    #endregion
}
