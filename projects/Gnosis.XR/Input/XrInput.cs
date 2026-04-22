using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 输入实现，管理 XR 动作绑定与触觉反馈
/// </summary>
public sealed class XrInput : IXrInput
{
    #region 字段

    private readonly Dictionary<string, IXrActionSet> _actionSets;
    private readonly Dictionary<XrTrackerType, XrHapticFeedback> _activeHaptics;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    public IXrSession Session { get; }

    /// <summary>
    /// 动作集列表
    /// </summary>
    public IReadOnlyList<IXrActionSet> ActionSets => _actionSets.Values.ToList().AsReadOnly();

    #endregion

    #region 事件

    /// <summary>
    /// 输入更新事件
    /// </summary>
    public event EventHandler<XrInputUpdateEventArgs>? InputUpdated;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用 XR 会话初始化 XR 输入
    /// </summary>
    /// <param name="session">XR 会话</param>
    public XrInput(IXrSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        _actionSets = new Dictionary<string, IXrActionSet>();
        _activeHaptics = new Dictionary<XrTrackerType, XrHapticFeedback>();
        _isDisposed = false;
    }

    #endregion

    #region IXrInput 实现

    /// <summary>
    /// 创建动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    /// <param name="priority">优先级</param>
    /// <returns>新创建的动作集</returns>
    public IXrActionSet CreateActionSet(string name, int priority = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();

        if (_actionSets.ContainsKey(name))
        {
            throw new InvalidOperationException($"已存在名为 '{name}' 的动作集");
        }

        var actionSet = new XrActionSet(name, priority);
        _actionSets[name] = actionSet;
        return actionSet;
    }

    /// <summary>
    /// 销毁动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    public void DestroyActionSet(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();

        _actionSets.Remove(name);
    }

    /// <summary>
    /// 获取指定名称的动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    /// <returns>动作集实例</returns>
    public IXrActionSet? GetActionSet(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _actionSets.GetValueOrDefault(name);
    }

    /// <summary>
    /// 同步所有动作集状态
    /// </summary>
    public void SyncActions()
    {
        ThrowIfDisposed();

        if (!Session.IsRunning)
        {
            return;
        }

        foreach (var actionSet in _actionSets.Values)
        {
            actionSet.Sync();
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        InputUpdated?.Invoke(this, new XrInputUpdateEventArgs(timestamp));
    }

    /// <summary>
    /// 向指定追踪器发送触觉反馈
    /// </summary>
    /// <param name="trackerType">追踪器类型</param>
    /// <param name="feedback">触觉反馈信息</param>
    public void SendHapticFeedback(XrTrackerType trackerType, XrHapticFeedback feedback)
    {
        ThrowIfDisposed();

        if (trackerType != XrTrackerType.LeftController && trackerType != XrTrackerType.RightController)
        {
            throw new ArgumentException($"追踪器类型 {trackerType} 不支持触觉反馈", nameof(trackerType));
        }

        _activeHaptics[trackerType] = feedback;
    }

    /// <summary>
    /// 停止指定追踪器的触觉反馈
    /// </summary>
    /// <param name="trackerType">追踪器类型</param>
    public void StopHapticFeedback(XrTrackerType trackerType)
    {
        ThrowIfDisposed();

        _activeHaptics.Remove(trackerType);
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 获取指定追踪器的活跃触觉反馈
    /// </summary>
    /// <param name="trackerType">追踪器类型</param>
    /// <returns>触觉反馈信息，不存在返回 null</returns>
    internal XrHapticFeedback? GetActiveHaptic(XrTrackerType trackerType)
    {
        return _activeHaptics.GetValueOrDefault(trackerType);
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
            throw new ObjectDisposedException(nameof(XrInput));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放 XR 输入资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _actionSets.Clear();
        _activeHaptics.Clear();
        _isDisposed = true;
    }

    #endregion
}
