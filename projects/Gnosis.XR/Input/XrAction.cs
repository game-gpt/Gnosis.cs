using Gnosis.XR.Tracking;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作实现，表示一个可绑定的 XR 输入动作
/// </summary>
public sealed class XrAction : IXrAction
{
    #region 字段

    private readonly List<XrActionBinding> _bindings;
    private XrActionState _currentState;
    private XrActionState _previousState;

    #endregion

    #region 属性

    /// <summary>
    /// 动作名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 动作类型
    /// </summary>
    public XrActionType ActionType { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 当前动作状态
    /// </summary>
    public XrActionState CurrentState => _currentState;

    /// <summary>
    /// 动作绑定列表
    /// </summary>
    public IReadOnlyList<XrActionBinding> Bindings => _bindings.AsReadOnly();

    #endregion

    #region 事件

    /// <summary>
    /// 动作状态变更事件
    /// </summary>
    public event EventHandler<XrActionStateChangedEventArgs>? StateChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用名称和类型初始化 XR 动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <param name="actionType">动作类型</param>
    public XrAction(string name, XrActionType actionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        ActionType = actionType;
        IsEnabled = true;
        _bindings = [];
        _currentState = new XrActionState();
        _previousState = new XrActionState();
    }

    #endregion

    #region IXrAction 实现

    /// <summary>
    /// 添加绑定
    /// </summary>
    /// <param name="binding">动作绑定</param>
    public void AddBinding(XrActionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        _bindings.Add(binding);
    }

    /// <summary>
    /// 移除绑定
    /// </summary>
    /// <param name="path">绑定路径</param>
    public void RemoveBinding(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _bindings.RemoveAll(b => b.Path == path);
    }

    /// <summary>
    /// 更新动作状态
    /// </summary>
    public void Update()
    {
        if (!IsEnabled)
        {
            return;
        }

        _previousState = _currentState;

        var hasActiveBinding = false;
        foreach (var binding in _bindings)
        {
            if (binding.IsActive)
            {
                hasActiveBinding = true;
                break;
            }
        }

        var newState = _currentState with
        {
            ChangedSinceLastFrame = false,
            IsActive = hasActiveBinding
        };

        if (_previousState.BooleanValue != newState.BooleanValue ||
            Math.Abs(_previousState.FloatValue - newState.FloatValue) > 0.001f ||
            _previousState.IsActive != newState.IsActive)
        {
            newState = newState with
            {
                ChangedSinceLastFrame = true,
                LastChangeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000
            };
        }

        _currentState = newState;

        if (_currentState.ChangedSinceLastFrame)
        {
            StateChanged?.Invoke(this, new XrActionStateChangedEventArgs(Name, _currentState, _previousState));
        }
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置动作状态（供 XR 运行时后端注入数据）
    /// </summary>
    /// <param name="state">新的动作状态</param>
    internal void SetState(XrActionState state)
    {
        _currentState = state;
    }

    #endregion
}
