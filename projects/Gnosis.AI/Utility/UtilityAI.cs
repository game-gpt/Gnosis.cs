namespace Gnosis.AI.Utility;

/// <summary>
/// 效用 AI 系统，基于效用评估器选择最优决策
/// </summary>
public sealed class UtilityAI
{
    #region 字段

    private readonly List<UtilityOption> _options = new();

    #endregion

    #region 属性

    /// <summary>
    /// 决策选项列表
    /// </summary>
    public IReadOnlyList<UtilityOption> Options => _options.AsReadOnly();

    /// <summary>
    /// 决策模式
    /// </summary>
    public UtilityDecisionMode DecisionMode { get; set; } = UtilityDecisionMode.Highest;

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加决策选项
    /// </summary>
    /// <param name="name">选项名称</param>
    /// <param name="evaluators">效用评估器列表</param>
    /// <param name="action">执行动作</param>
    public void AddOption(string name, List<IUtilityEvaluator> evaluators, Action action)
    {
        _options.Add(new UtilityOption(name, evaluators, action));
    }

    /// <summary>
    /// 移除决策选项
    /// </summary>
    /// <param name="name">选项名称</param>
    public void RemoveOption(string name)
    {
        _options.RemoveAll(o => o.Name == name);
    }

    /// <summary>
    /// 评估所有选项并选择最优决策
    /// </summary>
    /// <param name="context">评估上下文，提供各评估器所需的输入值</param>
    /// <returns>被选中的选项名称，无选项则返回 null</returns>
    public string? Decide(UtilityContext context)
    {
        if (_options.Count == 0)
        {
            return null;
        }

        float bestScore = float.MinValue;
        UtilityOption? bestOption = null;

        foreach (var option in _options)
        {
            float score = option.Evaluate(context);

            if (score > bestScore)
            {
                bestScore = score;
                bestOption = option;
            }
        }

        bestOption?.Execute();

        return bestOption?.Name;
    }

    /// <summary>
    /// 清空所有选项
    /// </summary>
    public void Clear()
    {
        _options.Clear();
    }

    #endregion
}

/// <summary>
/// 效用决策模式
/// </summary>
public enum UtilityDecisionMode : byte
{
    /// <summary>
    /// 选择效用最高的选项
    /// </summary>
    Highest = 0,

    /// <summary>
    /// 加权随机选择
    /// </summary>
    WeightedRandom = 1
}

/// <summary>
/// 效用评估上下文，为各评估器提供输入值
/// </summary>
public sealed class UtilityContext
{
    private readonly Dictionary<string, float> _values = new();

    /// <summary>
    /// 设置上下文值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SetValue(string key, float value)
    {
        _values[key] = value;
    }

    /// <summary>
    /// 获取上下文值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值，不存在则返回 0</returns>
    public float GetValue(string key)
    {
        return _values.GetValueOrDefault(key, 0f);
    }

    /// <summary>
    /// 清空上下文
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }
}

/// <summary>
/// 效用决策选项
/// </summary>
public sealed class UtilityOption
{
    #region 字段

    private readonly List<IUtilityEvaluator> _evaluators;
    private readonly Action _action;

    #endregion

    #region 属性

    /// <summary>
    /// 选项名称
    /// </summary>
    public string Name { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建效用决策选项
    /// </summary>
    /// <param name="name">选项名称</param>
    /// <param name="evaluators">效用评估器列表</param>
    /// <param name="action">执行动作</param>
    public UtilityOption(string name, List<IUtilityEvaluator> evaluators, Action action)
    {
        Name = name;
        _evaluators = evaluators;
        _action = action;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 评估选项的综合效用值
    /// </summary>
    /// <param name="context">评估上下文</param>
    /// <returns>综合效用值</returns>
    public float Evaluate(UtilityContext context)
    {
        if (_evaluators.Count == 0)
        {
            return 0f;
        }

        float totalScore = 0f;

        foreach (var evaluator in _evaluators)
        {
            float input = context.GetValue(evaluator.Name);
            float score = evaluator.Evaluate(input);
            totalScore += score;
        }

        return totalScore / _evaluators.Count;
    }

    /// <summary>
    /// 执行选项动作
    /// </summary>
    public void Execute()
    {
        _action();
    }

    #endregion
}
