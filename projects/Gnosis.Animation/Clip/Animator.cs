using Gnosis.Animation.IK;
using Gnosis.Animation.State;

namespace Gnosis.Animation.Clip;

public sealed class Animator : IAnimator
{
    #region 字段

    private readonly List<IAnimationLayer> _layers = new();
    private readonly List<IIKSolver> _ikSolvers = new();
    private readonly Dictionary<string, float> _floatParams = new();
    private readonly Dictionary<string, bool> _boolParams = new();
    private readonly HashSet<string> _triggers = new();
    private IAnimationStateMachine _stateMachine;

    #endregion

    #region 属性

    public ISkeleton? Skeleton { get; }

    public IReadOnlyList<IAnimationLayer> Layers => _layers;

    public IAnimationStateMachine StateMachine => _stateMachine;

    #endregion

    #region 构造函数

    public Animator(ISkeleton? skeleton = null)
    {
        Skeleton = skeleton;
        _stateMachine = new AnimationStateMachine("Default");
    }

    #endregion

    #region IAnimator 实现

    public void Play(string stateName)
    {
        _stateMachine.SetTrigger(stateName);
    }

    public void PlayInFixedTime(string stateName, float fixedTime)
    {
        _stateMachine.SetTrigger(stateName);
    }

    public void CrossFade(string stateName, float transitionDuration)
    {
        _stateMachine.SetTrigger(stateName);
    }

    public void SetFloat(string name, float value)
    {
        _floatParams[name] = value;
        _stateMachine.SetFloat(name, value);
    }

    public void SetBool(string name, bool value)
    {
        _boolParams[name] = value;
        _stateMachine.SetBool(name, value);
    }

    public void SetTrigger(string name)
    {
        _triggers.Add(name);
        _stateMachine.SetTrigger(name);
    }

    public float GetFloat(string name)
    {
        return _floatParams.GetValueOrDefault(name, 0f);
    }

    public bool GetBool(string name)
    {
        return _boolParams.TryGetValue(name, out var value) && value;
    }

    public void AddLayer(IAnimationLayer layer)
    {
        _layers.Add(layer ?? throw new ArgumentNullException(nameof(layer)));
    }

    public void AddIKSolver(IIKSolver solver)
    {
        _ikSolvers.Add(solver ?? throw new ArgumentNullException(nameof(solver)));
    }

    public void Update(float delta)
    {
        _stateMachine.Update(delta);

        foreach (var solver in _ikSolvers)
        {
            if (solver.IsActive && Skeleton is not null)
            {
                solver.Solve(Skeleton, null);
            }
        }
    }

    #endregion
}
