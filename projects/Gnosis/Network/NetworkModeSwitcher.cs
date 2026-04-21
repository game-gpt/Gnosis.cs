namespace Gnosis.Network;

/// <summary>
/// 网络模式切换器，支持运行时在单机、状态同步和帧同步模式间切换
/// </summary>
public sealed class NetworkModeSwitcher
{
    #region 字段

    private SyncMode _currentMode = SyncMode.None;

    #endregion

    #region 属性

    /// <summary>
    /// 当前同步模式
    /// </summary>
    public SyncMode CurrentMode => _currentMode;

    /// <summary>
    /// 关联的网络管理器
    /// </summary>
    public INetworkManager? Manager { get; private set; }

    /// <summary>
    /// 关联的状态同步系统
    /// </summary>
    public IStateSyncSystem? StateSync { get; private set; }

    /// <summary>
    /// 关联的帧同步系统
    /// </summary>
    public ILockstepSystem? Lockstep { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化网络模式切换器
    /// </summary>
    /// <param name="manager">网络管理器</param>
    /// <param name="stateSync">状态同步系统</param>
    /// <param name="lockstep">帧同步系统</param>
    public NetworkModeSwitcher(INetworkManager? manager = null, IStateSyncSystem? stateSync = null, ILockstepSystem? lockstep = null)
    {
        Manager = manager;
        StateSync = stateSync;
        Lockstep = lockstep;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 切换到指定同步模式
    /// </summary>
    /// <param name="mode">目标同步模式</param>
    public void SwitchTo(SyncMode mode)
    {
        throw new NotImplementedException("模式切换尚未实现");
    }

    /// <summary>
    /// 重置当前网络状态
    /// </summary>
    public void ResetState()
    {
        throw new NotImplementedException("模式切换尚未实现");
    }

    /// <summary>
    /// 获取指定模式所需的系统列表
    /// </summary>
    /// <param name="mode">同步模式</param>
    /// <returns>系统名称列表</returns>
    public IEnumerable<string> GetRequiredSystems(SyncMode mode)
    {
        return mode switch
        {
            SyncMode.None => Array.Empty<string>(),
            SyncMode.StateSync => new[] { "ServerMovement", "ClientPredictionMovement" },
            SyncMode.Lockstep => new[] { "LockstepCombat" },
            _ => Array.Empty<string>()
        };
    }

    #endregion
}
