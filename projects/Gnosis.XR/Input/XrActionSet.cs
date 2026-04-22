namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作集实现，管理一组相关的 XR 动作
/// </summary>
public sealed class XrActionSet : IXrActionSet
{
    #region 字段

    private readonly Dictionary<string, IXrAction> _actions;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 动作集名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 动作列表
    /// </summary>
    public IReadOnlyList<IXrAction> Actions => _actions.Values.ToList().AsReadOnly();

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用名称和优先级初始化 XR 动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    /// <param name="priority">优先级</param>
    public XrActionSet(string name, int priority = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Priority = priority;
        IsEnabled = true;
        _actions = new Dictionary<string, IXrAction>();
        _isDisposed = false;
    }

    #endregion

    #region IXrActionSet 实现

    /// <summary>
    /// 获取指定名称的动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <returns>动作实例</returns>
    public IXrAction? GetAction(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();

        return _actions.GetValueOrDefault(name);
    }

    /// <summary>
    /// 创建并添加一个新动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <param name="actionType">动作类型</param>
    /// <returns>新创建的动作</returns>
    public IXrAction CreateAction(string name, XrActionType actionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();

        if (_actions.ContainsKey(name))
        {
            throw new InvalidOperationException($"动作集 '{Name}' 中已存在名为 '{name}' 的动作");
        }

        var action = new XrAction(name, actionType);
        _actions[name] = action;
        return action;
    }

    /// <summary>
    /// 移除指定名称的动作
    /// </summary>
    /// <param name="name">动作名称</param>
    public void RemoveAction(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();

        _actions.Remove(name);
    }

    /// <summary>
    /// 同步动作状态
    /// </summary>
    public void Sync()
    {
        ThrowIfDisposed();

        if (!IsEnabled)
        {
            return;
        }

        foreach (var action in _actions.Values)
        {
            if (action.IsEnabled)
            {
                action.Update();
            }
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(XrActionSet));
        }
    }

    #endregion
}
