namespace Gnosis.AI.State;

/// <summary>
/// 层次状态机实现，支持子状态机和自动过渡检测
/// </summary>
public sealed class HierarchicalStateMachine : IStateMachine
{
    #region 字段

    private readonly Dictionary<string, IState> _stateMap = new();
    private readonly List<ITransition> _transitions = new();
    private readonly List<IState> _states = new();
    private IState? _currentState;
    private IState? _initialState;

    #endregion

    #region 属性

    /// <summary>
    /// 状态机名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public IState? CurrentState => _currentState;

    /// <summary>
    /// 所有状态
    /// </summary>
    public IReadOnlyList<IState> States => _states.AsReadOnly();

    /// <summary>
    /// 所有过渡
    /// </summary>
    public IReadOnlyList<ITransition> Transitions => _transitions.AsReadOnly();

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建层次状态机
    /// </summary>
    /// <param name="name">状态机名称</param>
    public HierarchicalStateMachine(string name)
    {
        Name = name;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="state">状态</param>
    public void AddState(IState state)
    {
        if (_stateMap.ContainsKey(state.Name))
        {
            return;
        }

        _stateMap[state.Name] = state;
        _states.Add(state);

        if (_initialState is null)
        {
            _initialState = state;
        }
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    /// <param name="stateName">状态名称</param>
    public void RemoveState(string stateName)
    {
        if (!_stateMap.TryGetValue(stateName, out var state))
        {
            return;
        }

        if (_currentState == state)
        {
            _currentState?.OnExit();
            _currentState = null;
        }

        if (_initialState == state)
        {
            _initialState = _states.Count > 1 ? _states[0] : null;
        }

        _stateMap.Remove(stateName);
        _states.Remove(state);

        _transitions.RemoveAll(t => t.FromState == stateName || t.ToState == stateName);
    }

    /// <summary>
    /// 添加过渡
    /// </summary>
    /// <param name="transition">过渡</param>
    public void AddTransition(ITransition transition)
    {
        _transitions.Add(transition);
    }

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    /// <param name="stateName">目标状态名称</param>
    /// <returns>是否切换成功</returns>
    public bool SwitchTo(string stateName)
    {
        if (!_stateMap.TryGetValue(stateName, out var targetState))
        {
            return false;
        }

        if (_currentState == targetState)
        {
            return false;
        }

        _currentState?.OnExit();
        _currentState = targetState;
        _currentState.OnEnter();

        return true;
    }

    /// <summary>
    /// 更新状态机
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        if (_currentState is null)
        {
            if (_initialState is not null)
            {
                _currentState = _initialState;
                _currentState.OnEnter();
            }
            else
            {
                return;
            }
        }

        EvaluateTransitions();

        _currentState.OnUpdate(delta);
    }

    /// <summary>
    /// 重置状态机
    /// </summary>
    public void Reset()
    {
        _currentState?.OnExit();
        _currentState = null;
    }

    /// <summary>
    /// 设置初始状态
    /// </summary>
    /// <param name="stateName">状态名称</param>
    public void SetInitialState(string stateName)
    {
        if (_stateMap.TryGetValue(stateName, out var state))
        {
            _initialState = state;
        }
    }

    /// <summary>
    /// 获取指定名称的状态
    /// </summary>
    /// <param name="stateName">状态名称</param>
    /// <returns>状态实例，不存在则返回 null</returns>
    public IState? GetState(string stateName)
    {
        return _stateMap.GetValueOrDefault(stateName);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 评估所有过渡条件，执行第一个满足条件的过渡
    /// </summary>
    private void EvaluateTransitions()
    {
        if (_currentState is null)
        {
            return;
        }

        foreach (var transition in _transitions)
        {
            if (transition.FromState != _currentState.Name)
            {
                continue;
            }

            if (!transition.CanTransition())
            {
                continue;
            }

            if (!_stateMap.TryGetValue(transition.ToState, out var targetState))
            {
                continue;
            }

            transition.OnTransition();

            _currentState.OnExit();
            _currentState = targetState;
            _currentState.OnEnter();

            return;
        }
    }

    #endregion
}
