using Gnosis.Animation.IK;

namespace Gnosis.Animation.State;

public sealed class AnimationStateMachine : IAnimationStateMachine
{
    #region 字段

    private readonly Dictionary<string, IAnimationState> _states = new();
    private readonly List<ITransition> _transitions = new();
    private readonly Dictionary<string, float> _floatParams = new();
    private readonly Dictionary<string, bool> _boolParams = new();
    private readonly HashSet<string> _triggers = new();
    private IAnimationState _currentState;
    private IAnimationState? _nextState;

    #endregion

    #region 属性

    public string Name { get; }

    public IAnimationState CurrentState => _currentState;

    public IAnimationState? NextState => _nextState;

    public float TransitionProgress { get; private set; }

    #endregion

    #region 构造函数

    public AnimationStateMachine(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _currentState = new AnimationState("Empty", "", 0f);
    }

    #endregion

    #region IAnimationStateMachine 实现

    public void AddState(IAnimationState state)
    {
        _states[state.Name] = state;

        if (_currentState.Name == "Empty")
        {
            _currentState = state;
        }
    }

    public void RemoveState(string name)
    {
        _states.Remove(name);
    }

    public void AddTransition(ITransition transition)
    {
        _transitions.Add(transition);
    }

    public void SetFloat(string name, float value)
    {
        _floatParams[name] = value;
    }

    public void SetBool(string name, bool value)
    {
        _boolParams[name] = value;
    }

    public void SetTrigger(string name)
    {
        _triggers.Add(name);
    }

    public float GetFloat(string name)
    {
        return _floatParams.GetValueOrDefault(name, 0f);
    }

    public bool GetBool(string name)
    {
        return _boolParams.TryGetValue(name, out var value) && value;
    }

    public void Update(float delta)
    {
        if (_currentState is AnimationState concreteState)
        {
            concreteState.Update(delta);
        }

        foreach (var transition in _transitions)
        {
            if (transition.SourceState != _currentState.Name)
            {
                continue;
            }

            if (_triggers.Remove(transition.DestinationState) || !transition.HasExitTime)
            {
                if (_states.TryGetValue(transition.DestinationState, out var state))
                {
                    _nextState = state;
                    TransitionProgress = 0f;
                    break;
                }
            }
        }

        if (_nextState is not null)
        {
            TransitionProgress += delta;

            var transitionDuration = 0.25f;

            if (TransitionProgress >= transitionDuration)
            {
                _currentState = _nextState;
                _nextState = null;
                TransitionProgress = 0f;
            }
        }
    }

    #endregion
}
