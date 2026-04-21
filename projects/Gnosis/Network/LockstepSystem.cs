using System;
using System.Collections.Generic;
using Gnosis.Network.Core;
using Gnosis.Network.Sync;

namespace Gnosis.Network;

/// <summary>
/// 帧同步系统，实现 Lockstep 模式下的输入收集、同步等待和确定性执行
/// </summary>
public sealed class LockstepSystem : ILockstepSystem
{
    #region 字段

    private readonly INetworkManager _networkManager;
    private readonly Dictionary<int, byte[]> _currentInputs = new();
    private readonly Dictionary<int, byte[]> _pendingInputs = new();
    private readonly Dictionary<int, HashSet<int>> _confirmedPlayers = new();
    private readonly List<int> _connectedPlayers = new();
    private readonly Dictionary<int, int> _stateHashes = new();
    private int _tickRate;
    private int _currentFrame;
    private int _inputBufferFrames = 2;
    private bool _deterministicMode = true;

    #endregion

    #region 属性

    /// <summary>
    /// 获取帧率
    /// </summary>
    public int TickRate => _tickRate;

    /// <summary>
    /// 获取当前帧号
    /// </summary>
    public int CurrentFrame => _currentFrame;

    /// <summary>
    /// 获取是否准备好推进到下一帧
    /// </summary>
    public bool ReadyToAdvance
    {
        get
        {
            if (_connectedPlayers.Count == 0)
            {
                return false;
            }

            if (!_confirmedPlayers.TryGetValue(_currentFrame, out var confirmed))
            {
                return false;
            }

            return confirmed.Count >= _connectedPlayers.Count;
        }
    }

    /// <summary>
    /// 获取或设置输入缓冲帧数
    /// </summary>
    public int InputBufferFrames
    {
        get => _inputBufferFrames;
        set => _inputBufferFrames = Math.Max(1, value);
    }

    /// <summary>
    /// 获取或设置是否启用确定性模式
    /// </summary>
    public bool DeterministicMode
    {
        get => _deterministicMode;
        set => _deterministicMode = value;
    }

    /// <summary>
    /// 获取已连接玩家数量
    /// </summary>
    public int ConnectedPlayerCount => _connectedPlayers.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 帧推进时触发
    /// </summary>
    public event Action<int, IReadOnlyDictionary<int, byte[]>>? OnFrameAdvanced;

    /// <summary>
    /// 状态哈希不匹配时触发（检测到不同步）
    /// </summary>
    public event Action<int, int, int>? OnDesyncDetected;

    /// <summary>
    /// 玩家输入已收集时触发
    /// </summary>
    public event Action<int, int>? OnPlayerInputCollected;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化帧同步系统
    /// </summary>
    /// <param name="networkManager">网络管理器</param>
    /// <param name="tickRate">帧率</param>
    public LockstepSystem(INetworkManager networkManager, int tickRate = 30)
    {
        _networkManager = networkManager;
        _tickRate = tickRate;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 帧同步更新，收集输入并执行确定性逻辑
    /// </summary>
    /// <param name="frame">当前帧号</param>
    /// <param name="inputs">所有玩家的输入数据</param>
    public void OnLockstepUpdate(int frame, IReadOnlyDictionary<int, byte[]> inputs)
    {
        _currentFrame = frame;

        foreach (var (playerId, inputData) in inputs)
        {
            _currentInputs[playerId] = inputData;
        }

        OnFrameAdvanced?.Invoke(frame, _currentInputs);
    }

    /// <summary>
    /// 注册玩家到帧同步系统
    /// </summary>
    /// <param name="playerId">玩家标识</param>
    public void RegisterPlayer(int playerId)
    {
        if (!_connectedPlayers.Contains(playerId))
        {
            _connectedPlayers.Add(playerId);
        }
    }

    /// <summary>
    /// 注销玩家
    /// </summary>
    /// <param name="playerId">玩家标识</param>
    public void UnregisterPlayer(int playerId)
    {
        _connectedPlayers.Remove(playerId);
        _currentInputs.Remove(playerId);
        _pendingInputs.Remove(playerId);

        foreach (var confirmed in _confirmedPlayers.Values)
        {
            confirmed.Remove(playerId);
        }
    }

    /// <summary>
    /// 提交玩家输入
    /// </summary>
    /// <param name="playerId">玩家标识</param>
    /// <param name="inputData">输入数据</param>
    public void SubmitInput(int playerId, byte[] inputData)
    {
        _pendingInputs[playerId] = inputData;

        if (!_confirmedPlayers.TryGetValue(_currentFrame, out var confirmed))
        {
            confirmed = new HashSet<int>();
            _confirmedPlayers[_currentFrame] = confirmed;
        }

        confirmed.Add(playerId);
        OnPlayerInputCollected?.Invoke(_currentFrame, playerId);
    }

    /// <summary>
    /// 提交状态哈希用于同步校验
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="hash">状态哈希值</param>
    public void SubmitStateHash(int frame, int hash)
    {
        if (_stateHashes.TryGetValue(frame, out var existingHash))
        {
            if (existingHash != hash)
            {
                OnDesyncDetected?.Invoke(frame, existingHash, hash);
            }
        }
        else
        {
            _stateHashes[frame] = hash;
        }
    }

    /// <summary>
    /// 尝试推进到下一帧
    /// </summary>
    /// <returns>是否成功推进</returns>
    public bool TryAdvance()
    {
        if (!ReadyToAdvance)
        {
            return false;
        }

        var inputs = new Dictionary<int, byte[]>(_pendingInputs);
        _currentFrame++;
        _pendingInputs.Clear();

        OnLockstepUpdate(_currentFrame, inputs);
        return true;
    }

    /// <summary>
    /// 获取指定玩家的当前输入
    /// </summary>
    /// <param name="playerId">玩家标识</param>
    /// <returns>输入数据，不存在则返回 null</returns>
    public byte[]? GetPlayerInput(int playerId)
    {
        return _currentInputs.GetValueOrDefault(playerId);
    }

    /// <summary>
    /// 获取指定帧的状态哈希
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <returns>状态哈希值，不存在则返回 null</returns>
    public int? GetStateHash(int frame)
    {
        return _stateHashes.GetValueOrDefault(frame);
    }

    /// <summary>
    /// 清除所有状态
    /// </summary>
    public void Clear()
    {
        _currentInputs.Clear();
        _pendingInputs.Clear();
        _confirmedPlayers.Clear();
        _connectedPlayers.Clear();
        _stateHashes.Clear();
        _currentFrame = 0;
    }

    #endregion
}
