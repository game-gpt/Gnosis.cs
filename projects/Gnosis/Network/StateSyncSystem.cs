using System;
using System.Collections.Generic;
using Gnosis.Core;
using Gnosis.Network.Core;
using Gnosis.Network.Sync;

namespace Gnosis.Network;

/// <summary>
/// 状态同步系统，实现服务器权威模式下的组件复制、客户端预测与和解
/// </summary>
public sealed class StateSyncSystem : IStateSyncSystem
{
    #region 字段

    private readonly INetworkManager _networkManager;
    private readonly IMessageSerializer _serializer;
    private readonly Dictionary<EntityId, byte[]> _serverStates = new();
    private readonly Dictionary<EntityId, PredictedState> _predictedStates = new();
    private readonly List<StateSnapshot> _pendingSnapshots = new();
    private readonly List<StateSnapshot> _predictionHistory = new();
    private float _reconciliationThreshold = 0.1f;
    private int _maxPredictionHistory = 60;

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置是否启用客户端预测
    /// </summary>
    public bool PredictionEnabled { get; set; } = true;

    /// <summary>
    /// 获取或设置和解误差阈值
    /// </summary>
    public float ReconciliationThreshold
    {
        get => _reconciliationThreshold;
        set => _reconciliationThreshold = Math.Max(0, value);
    }

    /// <summary>
    /// 获取服务器状态数量
    /// </summary>
    public int ServerStateCount => _serverStates.Count;

    /// <summary>
    /// 获取预测状态数量
    /// </summary>
    public int PredictedStateCount => _predictedStates.Count;

    /// <summary>
    /// 获取预测历史长度
    /// </summary>
    public int PredictionHistoryCount => _predictionHistory.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 服务器状态更新时触发
    /// </summary>
    public event Action<EntityId, byte[]>? OnServerStateReceived;

    /// <summary>
    /// 执行和解时触发
    /// </summary>
    public event Action<EntityId, float>? OnReconciliation;

    /// <summary>
    /// 预测误差超出阈值时触发
    /// </summary>
    public event Action<EntityId, float>? OnPredictionError;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化状态同步系统
    /// </summary>
    /// <param name="networkManager">网络管理器</param>
    /// <param name="serializer">消息序列化器</param>
    public StateSyncSystem(INetworkManager networkManager, IMessageSerializer serializer)
    {
        _networkManager = networkManager;
        _serializer = serializer;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 服务器端更新，计算权威状态并广播
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnServerUpdate(float delta)
    {
        if (!_networkManager.IsServer)
        {
            return;
        }

        foreach (var snapshot in _pendingSnapshots)
        {
            _serverStates[snapshot.EntityId] = snapshot.Data;
        }

        _pendingSnapshots.Clear();

        foreach (var (entityId, data) in _serverStates)
        {
            _networkManager.SendToAll(data, reliable: false);
        }
    }

    /// <summary>
    /// 客户端更新，执行本地预测
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnClientUpdate(float delta)
    {
        if (!_networkManager.IsClient || !PredictionEnabled)
        {
            return;
        }

        foreach (var (entityId, predicted) in _predictedStates)
        {
            var snapshot = new StateSnapshot(entityId, predicted.Data, delta);
            _predictionHistory.Add(snapshot);

            if (_predictionHistory.Count > _maxPredictionHistory)
            {
                _predictionHistory.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// 接收服务器状态，执行和解检查
    /// </summary>
    /// <param name="stateData">服务器状态数据</param>
    public void OnReceiveServerState(byte[] stateData)
    {
        if (stateData.Length < 16)
        {
            return;
        }

        var entityId = DecodeEntityId(stateData);
        var statePayload = ExtractPayload(stateData);

        _serverStates[entityId] = statePayload;
        OnServerStateReceived?.Invoke(entityId, statePayload);

        if (PredictionEnabled && _predictedStates.TryGetValue(entityId, out var predicted))
        {
            var error = CalculateError(predicted.Data, statePayload);

            if (error > _reconciliationThreshold)
            {
                OnPredictionError?.Invoke(entityId, error);
                Reconcile(entityId, statePayload);
            }
        }
    }

    /// <summary>
    /// 注册实体到状态同步系统
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="initialState">初始状态数据</param>
    public void RegisterEntity(EntityId entityId, byte[] initialState)
    {
        _serverStates[entityId] = initialState;
        _predictedStates[entityId] = new PredictedState(initialState);
    }

    /// <summary>
    /// 注销实体的状态同步
    /// </summary>
    /// <param name="entityId">实体标识</param>
    public void UnregisterEntity(EntityId entityId)
    {
        _serverStates.Remove(entityId);
        _predictedStates.Remove(entityId);
    }

    /// <summary>
    /// 更新实体的预测状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="data">新的预测状态数据</param>
    public void UpdatePredictedState(EntityId entityId, byte[] data)
    {
        if (_predictedStates.ContainsKey(entityId))
        {
            _predictedStates[entityId] = new PredictedState(data);
        }
    }

    /// <summary>
    /// 提交服务器端状态快照
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="data">状态数据</param>
    public void SubmitServerState(EntityId entityId, byte[] data)
    {
        _pendingSnapshots.Add(new StateSnapshot(entityId, data, 0));
    }

    /// <summary>
    /// 获取实体的服务器状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>服务器状态数据，不存在则返回 null</returns>
    public byte[]? GetServerState(EntityId entityId)
    {
        return _serverStates.GetValueOrDefault(entityId);
    }

    /// <summary>
    /// 获取实体的预测状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>预测状态数据，不存在则返回 null</returns>
    public byte[]? GetPredictedState(EntityId entityId)
    {
        return _predictedStates.TryGetValue(entityId, out var predicted) ? predicted.Data : null;
    }

    /// <summary>
    /// 清除所有状态
    /// </summary>
    public void Clear()
    {
        _serverStates.Clear();
        _predictedStates.Clear();
        _pendingSnapshots.Clear();
        _predictionHistory.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 执行和解，将预测状态校正为服务器状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="serverState">服务器状态数据</param>
    private void Reconcile(EntityId entityId, byte[] serverState)
    {
        if (_predictedStates.ContainsKey(entityId))
        {
            var oldPredicted = _predictedStates[entityId].Data;
            var error = CalculateError(oldPredicted, serverState);

            _predictedStates[entityId] = new PredictedState(serverState);
            OnReconciliation?.Invoke(entityId, error);
        }
    }

    /// <summary>
    /// 计算预测状态与服务器状态之间的误差
    /// </summary>
    /// <param name="predicted">预测状态</param>
    /// <param name="server">服务器状态</param>
    /// <returns>误差值</returns>
    private float CalculateError(byte[] predicted, byte[] server)
    {
        if (predicted.Length != server.Length)
        {
            return Math.Abs(predicted.Length - server.Length);
        }

        float error = 0;

        for (var i = 0; i < predicted.Length; i++)
        {
            error += Math.Abs(predicted[i] - server[i]);
        }

        return error / predicted.Length;
    }

    /// <summary>
    /// 从状态数据中解码实体标识
    /// </summary>
    /// <param name="data">状态数据（前 16 字节为 EntityId 的 GUID）</param>
    /// <returns>实体标识</returns>
    private EntityId DecodeEntityId(byte[] data)
    {
        var guidBytes = new byte[16];
        Array.Copy(data, 0, guidBytes, 0, 16);
        var guid = new Guid(guidBytes);
        return new EntityId(guid);
    }

    /// <summary>
    /// 从状态数据中提取载荷（跳过前 16 字节的 EntityId）
    /// </summary>
    /// <param name="data">状态数据</param>
    /// <returns>状态载荷</returns>
    private byte[] ExtractPayload(byte[] data)
    {
        if (data.Length <= 16)
        {
            return Array.Empty<byte>();
        }

        var payload = new byte[data.Length - 16];
        Array.Copy(data, 16, payload, 0, payload.Length);
        return payload;
    }

    #endregion

    #region 内部类型

    private readonly record struct PredictedState(byte[] Data);

    private readonly record struct StateSnapshot(EntityId EntityId, byte[] Data, float Delta);

    #endregion
}
