using System;
using System.Collections.Generic;
using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Core.Time;

namespace Gnosis.Network.Prediction;

/// <summary>
/// 客户端预测与和解系统，处理本地预测执行和服务器状态校正
/// </summary>
public sealed class PredictionSystem
{
    #region 字段

    private readonly Dictionary<int, PredictionRecord> _history = new();
    private readonly Dictionary<EntityId, byte[]> _currentPredictedStates = new();
    private readonly Dictionary<EntityId, float> _correctionInterpolations = new();
    private int _historyBufferSize = 64;

    #endregion

    #region 属性

    /// <summary>
    /// 预测历史缓冲区大小
    /// </summary>
    public int HistoryBufferSize
    {
        get => _historyBufferSize;
        set => _historyBufferSize = Math.Max(1, value);
    }

    /// <summary>
    /// 和解误差阈值，超过此值触发校正
    /// </summary>
    public float ReconciliationThreshold { get; set; } = 0.1f;

    /// <summary>
    /// 是否启用平滑校正
    /// </summary>
    public bool SmoothCorrectionEnabled { get; set; } = true;

    /// <summary>
    /// 平滑校正插值速度
    /// </summary>
    public float SmoothCorrectionSpeed { get; set; } = 10.0f;

    /// <summary>
    /// 上次和解时的误差值
    /// </summary>
    public float LastReconciliationError { get; private set; }

    /// <summary>
    /// 获取预测历史记录数量
    /// </summary>
    public int HistoryCount => _history.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 执行和解校正时触发
    /// </summary>
    public event Action<int, float>? OnReconciled;

    /// <summary>
    /// 预测误差超出阈值时触发
    /// </summary>
    public event Action<int, float>? OnPredictionError;

    #endregion

    #region 公共方法

    /// <summary>
    /// 记录预测状态到历史缓冲区
    /// </summary>
    /// <param name="frame">帧序号</param>
    /// <param name="stateData">预测状态数据</param>
    public void RecordPrediction(int frame, byte[] stateData)
    {
        _history[frame] = new PredictionRecord(frame, stateData, Timestamp.Now);

        if (_history.Count > _historyBufferSize)
        {
            var oldestFrame = int.MaxValue;

            foreach (var key in _history.Keys)
            {
                if (key < oldestFrame)
                {
                    oldestFrame = key;
                }
            }

            if (oldestFrame != int.MaxValue)
            {
                _history.Remove(oldestFrame);
            }
        }
    }

    /// <summary>
    /// 记录指定实体的预测状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="stateData">预测状态数据</param>
    public void RecordEntityPrediction(EntityId entityId, byte[] stateData)
    {
        _currentPredictedStates[entityId] = stateData;
    }

    /// <summary>
    /// 与服务器状态进行和解，检测预测误差
    /// </summary>
    /// <param name="serverFrame">服务器帧序号</param>
    /// <param name="serverState">服务器状态数据</param>
    /// <returns>是否需要校正</returns>
    public bool Reconcile(int serverFrame, byte[] serverState)
    {
        if (!_history.TryGetValue(serverFrame, out var prediction))
        {
            return false;
        }

        var error = CalculateError(prediction.StateData, serverState);
        LastReconciliationError = error;

        if (error > ReconciliationThreshold)
        {
            OnPredictionError?.Invoke(serverFrame, error);
            OnReconciled?.Invoke(serverFrame, error);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 与指定实体的服务器状态进行和解
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="serverState">服务器状态数据</param>
    /// <returns>是否需要校正</returns>
    public bool ReconcileEntity(EntityId entityId, byte[] serverState)
    {
        if (!_currentPredictedStates.TryGetValue(entityId, out var predicted))
        {
            return false;
        }

        var error = CalculateError(predicted, serverState);
        LastReconciliationError = error;

        if (error > ReconciliationThreshold)
        {
            _currentPredictedStates[entityId] = serverState;
            OnReconciled?.Invoke(0, error);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 清除指定帧之前的预测历史
    /// </summary>
    /// <param name="acknowledgedFrame">已确认的帧序号</param>
    public void ClearHistoryBefore(int acknowledgedFrame)
    {
        var keysToRemove = new List<int>();

        foreach (var key in _history.Keys)
        {
            if (key < acknowledgedFrame)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _history.Remove(key);
        }
    }

    /// <summary>
    /// 获取指定帧的预测状态
    /// </summary>
    /// <param name="frame">帧序号</param>
    /// <returns>预测状态数据，不存在则返回 null</returns>
    public byte[]? GetPredictedState(int frame)
    {
        return _history.TryGetValue(frame, out var record) ? record.StateData : null;
    }

    /// <summary>
    /// 清除所有预测历史和状态
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentPredictedStates.Clear();
        _correctionInterpolations.Clear();
        LastReconciliationError = 0;
    }

    #endregion

    #region 私有方法

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

        for (int i = 0; i < predicted.Length; i++)
        {
            error += Math.Abs(predicted[i] - server[i]);
        }

        return error / Math.Max(1, predicted.Length);
    }

    #endregion

    #region 内部类型

    private readonly record struct PredictionRecord(int Frame, byte[] StateData, Timestamp Timestamp);

    #endregion
}
