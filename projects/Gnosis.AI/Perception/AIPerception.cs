using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

/// <summary>
/// AI 感知系统实现，整合视觉、听觉、触觉等多种感知通道，
/// 并通过感知记忆管理已感知目标的历史信息
/// </summary>
public sealed class AIPerception : IAIPerception
{
    #region 字段

    private readonly List<IAISenseConfig> _senseConfigs = new();
    private readonly Dictionary<AISenseType, IAISense> _senses = new();
    private readonly List<IAIStimulusSource> _perceivedTargets = new();
    private readonly HashSet<IAIStimulusSource> _perceivedSet = new();
    private readonly IAISenseFactory _senseFactory;

    #endregion

    #region 属性

    /// <summary>
    /// 感知配置列表
    /// </summary>
    public IReadOnlyList<IAISenseConfig> SenseConfigs => _senseConfigs.AsReadOnly();

    /// <summary>
    /// 当前感知到的所有目标列表（去重后）
    /// </summary>
    public IReadOnlyList<IAIStimulusSource> PerceivedTargets => _perceivedTargets;

    /// <summary>
    /// 感知记忆系统
    /// </summary>
    public IPerceptionMemory Memory { get; }

    /// <summary>
    /// 感知者位置
    /// </summary>
    public Vector3 OwnerPosition { get; set; } = new(0, 0, 0);

    /// <summary>
    /// 感知者朝向（归一化方向向量）
    /// </summary>
    public Vector3 OwnerForward { get; set; } = new(0, 0, 1);

    /// <summary>
    /// 视线检测回调，返回 true 表示无遮挡
    /// </summary>
    public Func<Vector3, Vector3, bool>? LineOfSightCheck { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 AI 感知系统
    /// </summary>
    /// <param name="memoryCapacity">记忆最大容量</param>
    /// <param name="memoryDecayTime">记忆衰减时间（秒）</param>
    public AIPerception(int memoryCapacity = 16, float memoryDecayTime = 5.0f)
        : this(new PerceptionMemory(memoryCapacity, memoryDecayTime), DefaultAISenseFactory.Instance)
    {
    }

    /// <summary>
    /// 创建 AI 感知系统（使用自定义记忆系统）
    /// </summary>
    /// <param name="memory">感知记忆</param>
    public AIPerception(IPerceptionMemory memory)
        : this(memory, DefaultAISenseFactory.Instance)
    {
    }

    /// <summary>
    /// 创建 AI 感知系统（使用自定义工厂）
    /// </summary>
    /// <param name="memory">感知记忆</param>
    /// <param name="senseFactory">感知通道工厂</param>
    public AIPerception(IPerceptionMemory memory, IAISenseFactory senseFactory)
    {
        Memory = memory;
        _senseFactory = senseFactory;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加感知通道
    /// </summary>
    /// <param name="config">感知配置</param>
    public void AddSense(IAISenseConfig config)
    {
        if (_senseConfigs.Exists(c => c.SenseType == config.SenseType))
        {
            return;
        }

        _senseConfigs.Add(config);

        IAISense sense = _senseFactory.Create(config);
        _senses[config.SenseType] = sense;
    }

    /// <summary>
    /// 移除感知通道
    /// </summary>
    /// <param name="senseType">感知类型</param>
    public void RemoveSense(AISenseType senseType)
    {
        _senseConfigs.RemoveAll(c => c.SenseType == senseType);
        _senses.Remove(senseType);
    }

    /// <summary>
    /// 更新感知系统
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        SyncOwnerState();

        _perceivedTargets.Clear();
        _perceivedSet.Clear();

        foreach (var kvp in _senses)
        {
            IAISense sense = kvp.Value;
            sense.Update(delta);
            CollectPerceivedTargets(sense.PerceivedTargets);
        }

        UpdateMemory();
        Memory.Update(delta);
    }

    /// <summary>
    /// 查询目标是否被当前感知到
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <returns>是否被感知</returns>
    public bool IsTargetPerceived(IAIStimulusSource target)
    {
        return _perceivedSet.Contains(target);
    }

    /// <summary>
    /// 获取目标的最后已知位置（优先从当前感知获取，其次从记忆获取）
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <returns>最后已知位置，未感知则返回零向量</returns>
    public Vector3 GetLastKnownPosition(IAIStimulusSource target)
    {
        if (_perceivedSet.Contains(target))
        {
            return target.Position;
        }

        var memoryEntry = Memory.GetEntry(target);
        if (memoryEntry.HasValue)
        {
            return memoryEntry.Value.LastKnownPosition;
        }

        return new Vector3(0, 0, 0);
    }

    /// <summary>
    /// 注册可感知目标到所有感知通道
    /// </summary>
    /// <param name="source">刺激源</param>
    public void RegisterTarget(IAIStimulusSource source)
    {
        foreach (var kvp in _senses)
        {
            kvp.Value.RegisterTarget(source);
        }
    }

    /// <summary>
    /// 从所有感知通道注销目标
    /// </summary>
    /// <param name="source">刺激源</param>
    public void UnregisterTarget(IAIStimulusSource source)
    {
        foreach (var kvp in _senses)
        {
            kvp.Value.UnregisterTarget(source);
        }

        Memory.Remove(source);
    }

    /// <summary>
    /// 获取指定类型的感知通道
    /// </summary>
    /// <param name="senseType">感知类型</param>
    /// <returns>感知通道，不存在则返回 null</returns>
    public IAISense? GetSense(AISenseType senseType)
    {
        return _senses.GetValueOrDefault(senseType);
    }

    /// <summary>
    /// 获取指定类型的感知配置
    /// </summary>
    /// <param name="senseType">感知类型</param>
    /// <returns>感知配置，不存在则返回 null</returns>
    public IAISenseConfig? GetSenseConfig(AISenseType senseType)
    {
        return _senseConfigs.Find(c => c.SenseType == senseType);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 同步感知者状态到各感知通道
    /// </summary>
    private void SyncOwnerState()
    {
        foreach (var kvp in _senses)
        {
            kvp.Value.OwnerPosition = OwnerPosition;
        }
    }

    /// <summary>
    /// 收集感知到的目标（去重）
    /// </summary>
    private void CollectPerceivedTargets(IReadOnlyList<IAIStimulusSource> targets)
    {
        foreach (var target in targets)
        {
            if (_perceivedSet.Add(target))
            {
                _perceivedTargets.Add(target);
            }
        }
    }

    /// <summary>
    /// 更新感知记忆
    /// </summary>
    private void UpdateMemory()
    {
        foreach (var target in _perceivedTargets)
        {
            Memory.AddOrUpdate(target, target.Position);
        }
    }

    #endregion
}
