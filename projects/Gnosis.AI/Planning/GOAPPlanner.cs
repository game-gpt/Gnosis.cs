namespace Gnosis.AI.Planning;

/// <summary>
/// GOAP 规划器实现，使用 A* 反向搜索从目标状态回溯到当前状态
/// </summary>
public sealed class GOAPPlanner : IGOAPPlanner
{
    #region 字段

    private readonly List<IGOAPAction> _availableActions = new();
    private int _maxSearchDepth;

    #endregion

    #region 属性

    /// <summary>
    /// 可用动作集合
    /// </summary>
    public IReadOnlyList<IGOAPAction> AvailableActions => _availableActions.AsReadOnly();

    /// <summary>
    /// 最大搜索深度
    /// </summary>
    public int MaxSearchDepth
    {
        get => _maxSearchDepth;
        set => _maxSearchDepth = Math.Max(1, value);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 GOAP 规划器
    /// </summary>
    /// <param name="maxSearchDepth">最大搜索深度</param>
    public GOAPPlanner(int maxSearchDepth = 16)
    {
        _maxSearchDepth = maxSearchDepth;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加可用动作
    /// </summary>
    /// <param name="action">动作</param>
    public void AddAction(IGOAPAction action)
    {
        if (!_availableActions.Exists(a => a.Name == action.Name))
        {
            _availableActions.Add(action);
        }
    }

    /// <summary>
    /// 移除可用动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    public void RemoveAction(string actionName)
    {
        _availableActions.RemoveAll(a => a.Name == actionName);
    }

    /// <summary>
    /// 规划从当前状态到目标状态的动作序列
    /// </summary>
    /// <param name="currentState">当前世界状态</param>
    /// <param name="goalState">目标世界状态</param>
    /// <returns>动作序列，无法规划则返回 null</returns>
    public IReadOnlyList<IGOAPAction>? Plan(IWorldState currentState, IWorldState goalState)
    {
        if (currentState.Satisfies(goalState))
        {
            return new List<IGOAPAction>().AsReadOnly();
        }

        var openSet = new List<PlanNode>();
        var closedSet = new HashSet<string>();

        var startNode = new PlanNode
        {
            State = goalState.Clone(),
            GCost = 0,
            HCost = ComputeHeuristic(goalState, currentState),
            Actions = new List<IGOAPAction>()
        };

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => (a.GCost + a.HCost).CompareTo(b.GCost + b.HCost));

            var current = openSet[0];
            openSet.RemoveAt(0);

            if (current.Actions.Count >= _maxSearchDepth)
            {
                continue;
            }

            string stateKey = ComputeStateKey(current.State);

            if (closedSet.Contains(stateKey))
            {
                continue;
            }

            closedSet.Add(stateKey);

            if (currentState.Satisfies(current.State))
            {
                current.Actions.Reverse();
                return current.Actions.AsReadOnly();
            }

            foreach (var action in _availableActions)
            {
                if (!action.Effects.Satisfies(current.State) && !HasOverlap(action.Effects, current.State))
                {
                    continue;
                }

                var newState = current.State.Clone();

                ApplyEffectsBackward(newState, action);

                string newStateKey = ComputeStateKey(newState);

                if (closedSet.Contains(newStateKey))
                {
                    continue;
                }

                var newActions = new List<IGOAPAction>(current.Actions) { action };

                var neighbor = new PlanNode
                {
                    State = newState,
                    GCost = current.GCost + action.Cost,
                    HCost = ComputeHeuristic(newState, currentState),
                    Actions = newActions
                };

                openSet.Add(neighbor);
            }
        }

        return null;
    }

    /// <summary>
    /// 重置规划器
    /// </summary>
    public void Reset()
    {
        _availableActions.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 计算启发式函数值（当前状态与目标状态的差异数量）
    /// </summary>
    private static float ComputeHeuristic(IWorldState from, IWorldState to)
    {
        var diff = to.Diff(from);
        return diff.Count;
    }

    /// <summary>
    /// 检查动作效果是否与世界状态有重叠
    /// </summary>
    private static bool HasOverlap(IWorldState effects, IWorldState state)
    {
        var diff = effects.Diff(state);
        return diff.Count > 0;
    }

    /// <summary>
    /// 反向应用动作效果：将动作的前置条件合并到当前状态中
    /// </summary>
    private static void ApplyEffectsBackward(IWorldState state, IGOAPAction action)
    {
        var preconditions = action.Preconditions;

        if (preconditions is WorldState preState)
        {
            foreach (var kvp in GetStateEntries(preState))
            {
                state.Set(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// 获取世界状态的键值对
    /// </summary>
    private static IEnumerable<KeyValuePair<string, bool>> GetStateEntries(WorldState state)
    {
        var diff = state.Diff(new WorldState());
        foreach (var key in diff)
        {
            yield return new KeyValuePair<string, bool>(key, state.Get(key));
        }
    }

    /// <summary>
    /// 计算世界状态的唯一键（用于去重）
    /// </summary>
    private static string ComputeStateKey(IWorldState state)
    {
        if (state is not WorldState ws)
        {
            return state.GetHashCode().ToString();
        }

        var entries = GetStateEntries(ws).OrderBy(kvp => kvp.Key);
        var parts = new List<string>();

        foreach (var kvp in entries)
        {
            parts.Add($"{kvp.Key}={kvp.Value}");
        }

        return string.Join(",", parts);
    }

    #endregion

    #region 内部类型

    /// <summary>
    /// 规划节点
    /// </summary>
    private sealed class PlanNode
    {
        public IWorldState State { get; init; } = null!;
        public float GCost { get; init; }
        public float HCost { get; init; }
        public List<IGOAPAction> Actions { get; init; } = new();
    }

    #endregion
}
