using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 输入接口，管理 XR 动作绑定与触觉反馈
/// </summary>
public interface IXrInput : IDisposable
{
    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    IXrSession Session { get; }

    /// <summary>
    /// 动作集列表
    /// </summary>
    IReadOnlyList<IXrActionSet> ActionSets { get; }

    /// <summary>
    /// 输入更新事件
    /// </summary>
    event EventHandler<XrInputUpdateEventArgs>? InputUpdated;

    /// <summary>
    /// 创建动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    /// <param name="priority">优先级</param>
    /// <returns>新创建的动作集</returns>
    IXrActionSet CreateActionSet(string name, int priority = 0);

    /// <summary>
    /// 销毁动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    void DestroyActionSet(string name);

    /// <summary>
    /// 获取指定名称的动作集
    /// </summary>
    /// <param name="name">动作集名称</param>
    /// <returns>动作集实例</returns>
    IXrActionSet? GetActionSet(string name);

    /// <summary>
    /// 同步所有动作集状态（每帧调用）
    /// </summary>
    void SyncActions();

    /// <summary>
    /// 向指定追踪器发送触觉反馈
    /// </summary>
    /// <param name="trackerType">追踪器类型（左手柄/右手柄）</param>
    /// <param name="feedback">触觉反馈信息</param>
    void SendHapticFeedback(XrTrackerType trackerType, XrHapticFeedback feedback);

    /// <summary>
    /// 停止指定追踪器的触觉反馈
    /// </summary>
    /// <param name="trackerType">追踪器类型</param>
    void StopHapticFeedback(XrTrackerType trackerType);
}

/// <summary>
/// XR 输入更新事件参数
/// </summary>
public class XrInputUpdateEventArgs : EventArgs
{
    /// <summary>
    /// 更新时间戳（纳秒）
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// 初始化输入更新事件参数
    /// </summary>
    public XrInputUpdateEventArgs(long timestamp)
    {
        Timestamp = timestamp;
    }
}
