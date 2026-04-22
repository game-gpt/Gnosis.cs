using System;
using System.Collections.Generic;
using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;

namespace Gnosis.Network.Replication;

/// <summary>
/// 复制组，定义一组需要同步的实体
/// </summary>
public enum ReplicationGroup : byte
{
    /// <summary>
    /// 默认组
    /// </summary>
    Default = 0,

    /// <summary>
    /// 玩家角色组
    /// </summary>
    PlayerCharacter = 1,

    /// <summary>
    /// NPC 组
    /// </summary>
    Npc = 2,

    /// <summary>
    /// 场景物体组
    /// </summary>
    SceneObject = 3
}

/// <summary>
/// 实体复制状态
/// </summary>
public readonly record struct ReplicatedEntityState
{
    /// <summary>
    /// 实体标识
    /// </summary>
    public EntityId EntityId { get; init; }

    /// <summary>
    /// 复制组
    /// </summary>
    public ReplicationGroup Group { get; init; }

    /// <summary>
    /// 所有者连接标识
    /// </summary>
    public ConnectionId OwnerConnectionId { get; init; }

    /// <summary>
    /// 状态数据
    /// </summary>
    public ReadOnlyMemory<byte> StateData { get; init; }

    /// <summary>
    /// 上次更新的序列号
    /// </summary>
    public uint Sequence { get; init; }
}

/// <summary>
/// 状态同步系统，管理实体的网络复制、增量更新与所有权
/// </summary>
public sealed class ReplicationSystem
{
    #region 字段

    private readonly Dictionary<EntityId, ReplicatedEntityState> _replicatedEntities = new();
    private readonly Dictionary<EntityId, byte[]> _previousStates = new();
    private readonly Dictionary<EntityId, uint> _sequences = new();
    private readonly Dictionary<EntityId, ConnectionId> _ownership = new();
    private readonly Dictionary<ConnectionId, HashSet<EntityId>> _ownedEntities = new();
    private readonly List<EntityId> _pendingDestroy = [];
    private readonly IMessageSerializer _serializer;
    private bool _isServer;

    #endregion

    #region 属性

    /// <summary>
    /// 获取已复制的实体数量
    /// </summary>
    public int ReplicatedEntityCount => _replicatedEntities.Count;

    /// <summary>
    /// 获取或设置是否为服务器模式
    /// </summary>
    public bool IsServer
    {
        get => _isServer;
        set => _isServer = value;
    }

    /// <summary>
    /// 获取或设置增量同步的最小变化字节数阈值
    /// </summary>
    public int DeltaMinBytes { get; set; } = 1;

    /// <summary>
    /// 获取或设置是否启用增量同步
    /// </summary>
    public bool DeltaSyncEnabled { get; set; } = true;

    #endregion

    #region 事件

    /// <summary>
    /// 实体被注册到复制系统时触发
    /// </summary>
    public event Action<EntityId, ReplicationGroup>? OnEntityRegistered;

    /// <summary>
    /// 实体状态更新时触发
    /// </summary>
    public event Action<EntityId, ReadOnlyMemory<byte>>? OnStateUpdated;

    /// <summary>
    /// 实体所有权变更时触发
    /// </summary>
    public event Action<EntityId, ConnectionId, ConnectionId>? OnOwnershipChanged;

    /// <summary>
    /// 实体被销毁时触发
    /// </summary>
    public event Action<EntityId>? OnEntityDestroyed;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化状态同步系统
    /// </summary>
    /// <param name="serializer">消息序列化器</param>
    public ReplicationSystem(IMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    #endregion

    #region 公共方法 - 实体管理

    /// <summary>
    /// 注册实体到复制系统
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="group">复制组</param>
    /// <param name="initialState">初始状态数据</param>
    /// <param name="ownerConnectionId">所有者连接标识</param>
    public void RegisterEntity(EntityId entityId, ReplicationGroup group, byte[] initialState, ConnectionId ownerConnectionId = default)
    {
        if (_replicatedEntities.ContainsKey(entityId))
        {
            throw new InvalidOperationException($"实体 {entityId} 已注册到复制系统");
        }

        var sequence = GetNextSequence(entityId);
        var state = new ReplicatedEntityState
        {
            EntityId = entityId,
            Group = group,
            OwnerConnectionId = ownerConnectionId,
            StateData = initialState,
            Sequence = sequence
        };

        _replicatedEntities[entityId] = state;
        _previousStates[entityId] = initialState;
        _ownership[entityId] = ownerConnectionId;

        if (!_ownedEntities.ContainsKey(ownerConnectionId))
        {
            _ownedEntities[ownerConnectionId] = new HashSet<EntityId>();
        }

        _ownedEntities[ownerConnectionId].Add(entityId);

        OnEntityRegistered?.Invoke(entityId, group);
    }

    /// <summary>
    /// 注销实体
    /// </summary>
    /// <param name="entityId">实体标识</param>
    public void UnregisterEntity(EntityId entityId)
    {
        if (!_replicatedEntities.ContainsKey(entityId))
        {
            return;
        }

        if (_ownership.TryGetValue(entityId, out var ownerId) && _ownedEntities.TryGetValue(ownerId, out var owned))
        {
            owned.Remove(entityId);
        }

        _replicatedEntities.Remove(entityId);
        _previousStates.Remove(entityId);
        _sequences.Remove(entityId);
        _ownership.Remove(entityId);

        OnEntityDestroyed?.Invoke(entityId);
    }

    /// <summary>
    /// 标记实体为待销毁
    /// </summary>
    /// <param name="entityId">实体标识</param>
    public void MarkForDestroy(EntityId entityId)
    {
        if (_replicatedEntities.ContainsKey(entityId) && !_pendingDestroy.Contains(entityId))
        {
            _pendingDestroy.Add(entityId);
        }
    }

    /// <summary>
    /// 检查实体是否已注册
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>是否已注册</returns>
    public bool IsRegistered(EntityId entityId)
    {
        return _replicatedEntities.ContainsKey(entityId);
    }

    /// <summary>
    /// 获取实体的复制状态
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>复制状态，不存在则返回 null</returns>
    public ReplicatedEntityState? GetEntityState(EntityId entityId)
    {
        return _replicatedEntities.GetValueOrDefault(entityId);
    }

    #endregion

    #region 公共方法 - 状态更新

    /// <summary>
    /// 更新实体状态（服务器端）
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="stateData">新的状态数据</param>
    public void UpdateState(EntityId entityId, byte[] stateData)
    {
        if (!_replicatedEntities.TryGetValue(entityId, out var existing))
        {
            return;
        }

        var sequence = GetNextSequence(entityId);
        var updated = existing with
        {
            StateData = stateData,
            Sequence = sequence
        };

        _replicatedEntities[entityId] = updated;
        OnStateUpdated?.Invoke(entityId, stateData);
    }

    /// <summary>
    /// 接收服务器状态更新（客户端）
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="stateData">服务器状态数据</param>
    /// <param name="sequence">序列号</param>
    public void ReceiveStateUpdate(EntityId entityId, byte[] stateData, uint sequence)
    {
        if (!_replicatedEntities.TryGetValue(entityId, out var existing))
        {
            return;
        }

        if (sequence <= existing.Sequence)
        {
            return;
        }

        var updated = existing with
        {
            StateData = stateData,
            Sequence = sequence
        };

        _replicatedEntities[entityId] = updated;
        _previousStates[entityId] = stateData;
        OnStateUpdated?.Invoke(entityId, stateData);
    }

    /// <summary>
    /// 计算增量更新数据
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>增量数据，无变化则返回空</returns>
    public ReadOnlyMemory<byte> ComputeDelta(EntityId entityId)
    {
        if (!_replicatedEntities.TryGetValue(entityId, out var current))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        if (!_previousStates.TryGetValue(entityId, out var previous))
        {
            return current.StateData;
        }

        if (!DeltaSyncEnabled)
        {
            return current.StateData;
        }

        var currentState = current.StateData.Span;
        var previousState = previous.AsSpan();

        if (currentState.Length != previousState.Length)
        {
            return current.StateData;
        }

        return ComputeDeltaInternal(previousState, currentState);
    }

    /// <summary>
    /// 应用增量更新数据
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="deltaData">增量数据</param>
    public void ApplyDelta(EntityId entityId, ReadOnlySpan<byte> deltaData)
    {
        if (!_replicatedEntities.TryGetValue(entityId, out var existing))
        {
            return;
        }

        if (!_previousStates.TryGetValue(entityId, out var previous))
        {
            var restoredState = deltaData.ToArray();
            UpdateState(entityId, restoredState);
            return;
        }

        var fullState = ApplyDeltaInternal(previous, deltaData);
        UpdateState(entityId, fullState);
    }

    /// <summary>
    /// 收集所有需要同步的实体状态
    /// </summary>
    /// <returns>需要同步的实体状态列表</returns>
    public IReadOnlyList<ReplicatedEntityState> CollectDirtyEntities()
    {
        var dirty = new List<ReplicatedEntityState>();

        foreach (var (entityId, state) in _replicatedEntities)
        {
            if (_previousStates.TryGetValue(entityId, out var previous))
            {
                if (!HasStateChanged(previous, state.StateData.Span))
                {
                    continue;
                }
            }

            dirty.Add(state);
            _previousStates[entityId] = state.StateData.ToArray();
        }

        return dirty;
    }

    #endregion

    #region 公共方法 - 所有权

    /// <summary>
    /// 获取实体的所有者
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <returns>所有者连接标识</returns>
    public ConnectionId GetOwner(EntityId entityId)
    {
        return _ownership.GetValueOrDefault(entityId);
    }

    /// <summary>
    /// 检查指定连接是否拥有实体
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="connectionId">连接标识</param>
    /// <returns>是否拥有</returns>
    public bool IsOwner(EntityId entityId, ConnectionId connectionId)
    {
        return _ownership.TryGetValue(entityId, out var owner) && owner.Equals(connectionId);
    }

    /// <summary>
    /// 转移实体所有权
    /// </summary>
    /// <param name="entityId">实体标识</param>
    /// <param name="newOwner">新所有者连接标识</param>
    public void TransferOwnership(EntityId entityId, ConnectionId newOwner)
    {
        if (!_ownership.TryGetValue(entityId, out var oldOwner))
        {
            return;
        }

        if (oldOwner.Equals(newOwner))
        {
            return;
        }

        if (_ownedEntities.TryGetValue(oldOwner, out var oldOwned))
        {
            oldOwned.Remove(entityId);
        }

        _ownership[entityId] = newOwner;

        if (!_ownedEntities.ContainsKey(newOwner))
        {
            _ownedEntities[newOwner] = new HashSet<EntityId>();
        }

        _ownedEntities[newOwner].Add(entityId);

        if (_replicatedEntities.TryGetValue(entityId, out var state))
        {
            _replicatedEntities[entityId] = state with { OwnerConnectionId = newOwner };
        }

        OnOwnershipChanged?.Invoke(entityId, oldOwner, newOwner);
    }

    /// <summary>
    /// 获取指定连接拥有的所有实体
    /// </summary>
    /// <param name="connectionId">连接标识</param>
    /// <returns>拥有的实体标识集合</returns>
    public IReadOnlySet<EntityId> GetOwnedEntities(ConnectionId connectionId)
    {
        return _ownedEntities.GetValueOrDefault(connectionId, new HashSet<EntityId>());
    }

    /// <summary>
    /// 移除指定连接拥有的所有实体
    /// </summary>
    /// <param name="connectionId">断开连接的标识</param>
    public void RemoveOwnedEntities(ConnectionId connectionId)
    {
        if (!_ownedEntities.TryGetValue(connectionId, out var owned))
        {
            return;
        }

        foreach (var entityId in owned)
        {
            _replicatedEntities.Remove(entityId);
            _previousStates.Remove(entityId);
            _sequences.Remove(entityId);
            _ownership.Remove(entityId);
            OnEntityDestroyed?.Invoke(entityId);
        }

        _ownedEntities.Remove(connectionId);
    }

    #endregion

    #region 公共方法 - 生命周期

    /// <summary>
    /// 处理待销毁的实体
    /// </summary>
    public void ProcessPendingDestroys()
    {
        foreach (var entityId in _pendingDestroy)
        {
            UnregisterEntity(entityId);
        }

        _pendingDestroy.Clear();
    }

    /// <summary>
    /// 清除所有状态
    /// </summary>
    public void Clear()
    {
        _replicatedEntities.Clear();
        _previousStates.Clear();
        _sequences.Clear();
        _ownership.Clear();
        _ownedEntities.Clear();
        _pendingDestroy.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取实体的下一个序列号
    /// </summary>
    private uint GetNextSequence(EntityId entityId)
    {
        if (!_sequences.TryGetValue(entityId, out var current))
        {
            current = 0;
        }

        var next = current + 1;
        _sequences[entityId] = next;
        return next;
    }

    /// <summary>
    /// 检查状态是否发生变化
    /// </summary>
    private bool HasStateChanged(byte[] previous, ReadOnlySpan<byte> current)
    {
        if (previous.Length != current.Length)
        {
            return true;
        }

        for (var i = 0; i < previous.Length; i++)
        {
            if (previous[i] != current[i])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 计算增量数据（仅包含变化的字节）
    /// </summary>
    /// <remarks>
    /// 增量格式：[2字节 变化块数量] [每块: 2字节偏移 + 1字节长度 + N字节数据]
    /// </remarks>
    private static ReadOnlyMemory<byte> ComputeDeltaInternal(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
    {
        var changes = new List<(int Offset, int Length, byte[] Data)>();
        var i = 0;

        while (i < current.Length)
        {
            if (current[i] == previous[i])
            {
                i++;
                continue;
            }

            var start = i;

            while (i < current.Length && current[i] != previous[i])
            {
                i++;
            }

            var length = i - start;
            var data = current.Slice(start, length).ToArray();
            changes.Add((start, length, data));
        }

        if (changes.Count == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var totalSize = 2;

        foreach (var (_, length, _) in changes)
        {
            totalSize += 2 + 1 + length;
        }

        var result = new byte[totalSize];
        var pos = 0;

        result[pos++] = (byte)(changes.Count >> 8);
        result[pos++] = (byte)(changes.Count & 0xFF);

        foreach (var (offset, length, data) in changes)
        {
            result[pos++] = (byte)(offset >> 8);
            result[pos++] = (byte)(offset & 0xFF);
            result[pos++] = (byte)length;
            Array.Copy(data, 0, result, pos, length);
            pos += length;
        }

        return result;
    }

    /// <summary>
    /// 应用增量数据
    /// </summary>
    private static byte[] ApplyDeltaInternal(byte[] previous, ReadOnlySpan<byte> delta)
    {
        if (delta.Length < 2)
        {
            return previous;
        }

        var result = new byte[previous.Length];
        Array.Copy(previous, result, previous.Length);

        var blockCount = (delta[0] << 8) | delta[1];
        var pos = 2;

        for (var b = 0; b < blockCount && pos + 3 <= delta.Length; b++)
        {
            var offset = (delta[pos] << 8) | delta[pos + 1];
            var length = delta[pos + 2];
            pos += 3;

            if (offset + length > result.Length || pos + length > delta.Length)
            {
                break;
            }

            delta.Slice(pos, length).CopyTo(result.AsSpan(offset, length));
            pos += length;
        }

        return result;
    }

    #endregion
}
