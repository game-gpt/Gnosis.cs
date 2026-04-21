using System.Collections.Generic;

namespace Gnosis.Network;

/// <summary>
/// Lockstep 帧同步系统，实现输入收集、同步等待和确定性执行
/// </summary>
public sealed class LockstepSystem : ILockstepSystem
{
    #region 属性

    /// <summary>
    /// 帧率（每秒逻辑帧数）
    /// </summary>
    public int TickRate { get; }

    /// <summary>
    /// 当前逻辑帧号
    /// </summary>
    public int CurrentFrame { get; private set; }

    /// <summary>
    /// 是否准备好推进到下一帧
    /// </summary>
    public bool ReadyToAdvance { get; private set; }

    /// <summary>
    /// 最大等待帧数，超过后强制推进
    /// </summary>
    public int MaxWaitFrames { get; set; } = 5;

    /// <summary>
    /// 是否启用回滚
    /// </summary>
    public bool RollbackEnabled { get; set; } = true;

    /// <summary>
    /// 回滚最大帧数
    /// </summary>
    public int MaxRollbackFrames { get; set; } = 7;

    /// <summary>
    /// 等待输入的玩家集合
    /// </summary>
    public IReadOnlySet<int> PendingPlayers => _pendingPlayers;

    private readonly HashSet<int> _pendingPlayers = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化帧同步系统
    /// </summary>
    /// <param name="tickRate">帧率</param>
    public LockstepSystem(int tickRate = 30)
    {
        TickRate = tickRate;
    }

    #endregion

    #region ILockstepSystem 实现

    /// <summary>
    /// 执行帧同步更新，所有客户端在相同帧执行相同逻辑
    /// </summary>
    /// <param name="frame">当前帧号</param>
    /// <param name="inputs">所有玩家的输入数据</param>
    public void OnLockstepUpdate(int frame, IReadOnlyDictionary<int, byte[]> inputs)
    {
        throw new NotImplementedException("帧同步系统尚未实现");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 提交本地玩家输入
    /// </summary>
    /// <param name="playerId">玩家标识</param>
    /// <param name="inputData">输入数据</param>
    public void SubmitInput(int playerId, byte[] inputData)
    {
        throw new NotImplementedException("帧同步系统尚未实现");
    }

    /// <summary>
    /// 计算当前状态哈希，用于同步校验
    /// </summary>
    /// <returns>状态哈希值</returns>
    public uint CalculateStateHash()
    {
        throw new NotImplementedException("帧同步系统尚未实现");
    }

    /// <summary>
    /// 回滚到指定帧并重新执行
    /// </summary>
    /// <param name="targetFrame">目标帧号</param>
    public void RollbackTo(int targetFrame)
    {
        throw new NotImplementedException("帧同步系统尚未实现");
    }

    #endregion
}
