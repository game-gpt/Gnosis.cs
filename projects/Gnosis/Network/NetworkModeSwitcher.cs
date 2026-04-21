using System;

namespace Gnosis.Network;

/// <summary>
/// 网络模式切换器，实现运行时同步模式切换
/// </summary>
public sealed class NetworkModeSwitcher
{
    #region 字段

    private readonly INetworkManager _networkManager;
    private SyncMode _currentMode = SyncMode.None;
    private IStateSyncSystem? _stateSyncSystem;
    private ILockstepSystem? _lockstepSystem;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前同步模式
    /// </summary>
    public SyncMode CurrentMode => _currentMode;

    /// <summary>
    /// 获取或设置状态同步系统
    /// </summary>
    public IStateSyncSystem? StateSyncSystem
    {
        get => _stateSyncSystem;
        set => _stateSyncSystem = value;
    }

    /// <summary>
    /// 获取或设置帧同步系统
    /// </summary>
    public ILockstepSystem? LockstepSystem
    {
        get => _lockstepSystem;
        set => _lockstepSystem = value;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 模式切换前触发
    /// </summary>
    public event Action<SyncMode, SyncMode>? OnModeChanging;

    /// <summary>
    /// 模式切换后触发
    /// </summary>
    public event Action<SyncMode>? OnModeChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化网络模式切换器
    /// </summary>
    /// <param name="networkManager">网络管理器</param>
    public NetworkModeSwitcher(INetworkManager networkManager)
    {
        _networkManager = networkManager;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 切换到指定同步模式
    /// </summary>
    /// <param name="mode">目标同步模式</param>
    /// <exception cref="InvalidOperationException">网络管理器未初始化时抛出</exception>
    public void SwitchTo(SyncMode mode)
    {
        if (_currentMode == mode)
        {
            return;
        }

        OnModeChanging?.Invoke(_currentMode, mode);

        DisableCurrentSystems();
        ResetNetworkState();

        _currentMode = mode;

        EnableCurrentSystems();

        OnModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// 切换到单机模式
    /// </summary>
    public void SwitchToOffline()
    {
        SwitchTo(SyncMode.None);
    }

    /// <summary>
    /// 切换到状态同步模式
    /// </summary>
    public void SwitchToStateSync()
    {
        SwitchTo(SyncMode.StateSync);
    }

    /// <summary>
    /// 切换到帧同步模式
    /// </summary>
    public void SwitchToLockstep()
    {
        SwitchTo(SyncMode.Lockstep);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 禁用当前模式的系统
    /// </summary>
    private void DisableCurrentSystems()
    {
        if (_currentMode == SyncMode.StateSync && _stateSyncSystem is not null)
        {
            _stateSyncSystem.PredictionEnabled = false;
        }
    }

    /// <summary>
    /// 重置网络状态
    /// </summary>
    private void ResetNetworkState()
    {
        if (_networkManager.IsServer || _networkManager.IsClient)
        {
            _networkManager.LeaveLobby();
        }
    }

    /// <summary>
    /// 启用当前模式的系统
    /// </summary>
    private void EnableCurrentSystems()
    {
        switch (_currentMode)
        {
            case SyncMode.StateSync:
                if (_stateSyncSystem is not null)
                {
                    _stateSyncSystem.PredictionEnabled = true;
                }
                break;

            case SyncMode.Lockstep:
                break;
        }
    }

    #endregion
}
