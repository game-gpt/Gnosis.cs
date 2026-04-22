using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnosis.Network.Lobby;

/// <summary>
/// 大厅服务默认实现，提供本地内存中的房间管理与匹配
/// </summary>
public sealed class LobbyService : ILobbyService, IRoomManager, IMatchmaker
{
    #region 字段

    private readonly Dictionary<string, LobbyRoom> _rooms = new();
    private LobbyRoom? _currentRoom;
    private LobbyMember? _localMember;
    private MatchCriteria? _currentMatchCriteria;
    private DateTime _matchStartTime;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前所在的房间
    /// </summary>
    public LobbyRoom? CurrentRoom => _currentRoom;

    /// <summary>
    /// 获取当前房间的所有成员
    /// </summary>
    public IReadOnlyList<LobbyMember> Members => _currentRoom?.Members ?? [];

    /// <summary>
    /// 是否已连接到大厅服务
    /// </summary>
    public bool IsConnected => _localMember is not null;

    /// <summary>
    /// 是否正在匹配中
    /// </summary>
    public bool IsMatching => _currentMatchCriteria is not null;

    /// <summary>
    /// 获取房间总数
    /// </summary>
    public int RoomCount => _rooms.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 房间创建时触发
    /// </summary>
    public event Action<LobbyRoom>? OnRoomCreated;

    /// <summary>
    /// 加入房间时触发
    /// </summary>
    public event Action<LobbyRoom, LobbyMember>? OnMemberJoined;

    /// <summary>
    /// 离开房间时触发
    /// </summary>
    public event Action<LobbyRoom, LobbyMember>? OnMemberLeft;

    /// <summary>
    /// 房间销毁时触发
    /// </summary>
    public event Action<string>? OnRoomDestroyed;

    /// <summary>
    /// 房间属性更新时触发
    /// </summary>
    public event Action<LobbyRoom>? OnRoomUpdated;

    /// <summary>
    /// 匹配成功时触发
    /// </summary>
    public event Action<LobbyRoom>? OnMatchFound;

    /// <summary>
    /// 匹配超时时触发
    /// </summary>
    public event Action? OnMatchTimeout;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化大厅服务
    /// </summary>
    /// <param name="localMemberId">本地成员标识</param>
    /// <param name="localMemberName">本地成员名称</param>
    public LobbyService(string localMemberId, string localMemberName)
    {
        _localMember = new LobbyMember(localMemberId, localMemberName);
    }

    #endregion

    #region ILobbyService 实现

    /// <summary>
    /// 创建房间
    /// </summary>
    /// <param name="options">房间创建选项</param>
    /// <returns>创建的房间信息</returns>
    public LobbyRoom CreateRoom(LobbyRoomOptions options)
    {
        if (_localMember is null)
        {
            throw new InvalidOperationException("大厅服务未连接");
        }

        if (_currentRoom is not null)
        {
            throw new InvalidOperationException("已在房间中，请先离开当前房间");
        }

        var roomId = Guid.NewGuid().ToString("N")[..8];
        var room = new LobbyRoom(roomId, options.Name, _localMember.MemberId, options.MaxPlayers, options.Password is not null);

        if (options.Properties is not null)
        {
            foreach (var (key, value) in options.Properties)
            {
                room.Properties[key] = value;
            }
        }

        var hostMember = new LobbyMember(_localMember.MemberId, _localMember.Name, isHost: true);
        room.Members.Add(hostMember);

        _rooms[roomId] = room;
        _currentRoom = room;

        OnRoomCreated?.Invoke(room);
        OnMemberJoined?.Invoke(room, hostMember);

        return room;
    }

    /// <summary>
    /// 加入指定房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="password">房间密码（可选）</param>
    /// <returns>加入的房间信息</returns>
    public LobbyRoom JoinRoom(string roomId, string? password = null)
    {
        if (_localMember is null)
        {
            throw new InvalidOperationException("大厅服务未连接");
        }

        if (_currentRoom is not null)
        {
            throw new InvalidOperationException("已在房间中，请先离开当前房间");
        }

        if (!_rooms.TryGetValue(roomId, out var room))
        {
            throw new InvalidOperationException($"房间 {roomId} 不存在");
        }

        if (room.IsFull)
        {
            throw new InvalidOperationException("房间已满");
        }

        var member = new LobbyMember(_localMember.MemberId, _localMember.Name);
        room.Members.Add(member);
        _currentRoom = room;

        OnMemberJoined?.Invoke(room, member);

        return room;
    }

    /// <summary>
    /// 离开当前房间
    /// </summary>
    public void LeaveRoom()
    {
        if (_currentRoom is null || _localMember is null)
        {
            return;
        }

        var member = _currentRoom.Members.FirstOrDefault(m => m.MemberId == _localMember.MemberId);

        if (member is not null)
        {
            _currentRoom.Members.Remove(member);
            OnMemberLeft?.Invoke(_currentRoom, member);
        }

        if (_currentRoom.Members.Count == 0)
        {
            var roomId = _currentRoom.RoomId;
            _rooms.Remove(roomId);
            OnRoomDestroyed?.Invoke(roomId);
        }
        else if (_currentRoom.HostId == _localMember.MemberId)
        {
            var newHost = _currentRoom.Members[0];
            newHost.IsHost = true;
            _currentRoom.HostId = newHost.MemberId;
        }

        _currentRoom = null;
    }

    /// <summary>
    /// 搜索房间
    /// </summary>
    /// <param name="filter">搜索过滤条件</param>
    /// <returns>匹配的房间列表</returns>
    public IReadOnlyList<LobbyRoom> SearchRooms(LobbySearchFilter? filter = null)
    {
        var rooms = _rooms.Values.AsEnumerable();

        if (filter is not null)
        {
            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                rooms = rooms.Where(r => r.Name.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.MinMaxPlayers.HasValue)
            {
                rooms = rooms.Where(r => r.MaxPlayers >= filter.MinMaxPlayers.Value);
            }

            if (filter.MaxMaxPlayers.HasValue)
            {
                rooms = rooms.Where(r => r.MaxPlayers <= filter.MaxMaxPlayers.Value);
            }

            if (filter.OnlyAvailable)
            {
                rooms = rooms.Where(r => !r.IsFull);
            }

            if (filter.OnlyNoPassword)
            {
                rooms = rooms.Where(r => !r.HasPassword);
            }

            if (filter.PropertyFilters is not null)
            {
                rooms = rooms.Where(r => filter.PropertyFilters.All(f => r.Properties.ContainsKey(f.Key) && r.Properties[f.Key] == f.Value));
            }
        }

        return rooms.ToList();
    }

    #endregion

    #region IRoomManager 实现

    /// <summary>
    /// 获取所有房间
    /// </summary>
    public IReadOnlyList<LobbyRoom> GetAllRooms()
    {
        return _rooms.Values.ToList();
    }

    /// <summary>
    /// 获取指定房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <returns>房间信息，不存在则返回 null</returns>
    public LobbyRoom? GetRoom(string roomId)
    {
        return _rooms.GetValueOrDefault(roomId);
    }

    /// <summary>
    /// 更新房间信息
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="updates">更新数据</param>
    public void UpdateRoom(string roomId, Dictionary<string, string> updates)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            return;
        }

        foreach (var (key, value) in updates)
        {
            room.Properties[key] = value;
        }

        OnRoomUpdated?.Invoke(room);
    }

    /// <summary>
    /// 销毁房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    public void DestroyRoom(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            return;
        }

        foreach (var member in room.Members.ToList())
        {
            OnMemberLeft?.Invoke(room, member);
        }

        _rooms.Remove(roomId);

        if (_currentRoom?.RoomId == roomId)
        {
            _currentRoom = null;
        }

        OnRoomDestroyed?.Invoke(roomId);
    }

    /// <summary>
    /// 踢出房间成员
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="memberId">成员标识</param>
    public void KickMember(string roomId, string memberId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            return;
        }

        var member = room.Members.FirstOrDefault(m => m.MemberId == memberId);

        if (member is null)
        {
            return;
        }

        room.Members.Remove(member);
        OnMemberLeft?.Invoke(room, member);
    }

    #endregion

    #region IMatchmaker 实现

    /// <summary>
    /// 加入匹配队列
    /// </summary>
    /// <param name="criteria">匹配条件</param>
    public void StartMatching(MatchCriteria criteria)
    {
        if (_localMember is null)
        {
            throw new InvalidOperationException("大厅服务未连接");
        }

        if (IsMatching)
        {
            throw new InvalidOperationException("已在匹配中");
        }

        _currentMatchCriteria = criteria;
        _matchStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消匹配
    /// </summary>
    public void CancelMatching()
    {
        _currentMatchCriteria = null;
    }

    /// <summary>
    /// 更新匹配状态（应在每帧调用）
    /// </summary>
    public void UpdateMatching()
    {
        if (!IsMatching || _currentMatchCriteria is null || _localMember is null)
        {
            return;
        }

        var elapsed = (DateTime.UtcNow - _matchStartTime).TotalSeconds;

        if (elapsed > _currentMatchCriteria.TimeoutSeconds)
        {
            _currentMatchCriteria = null;
            OnMatchTimeout?.Invoke();
            return;
        }

        var availableRooms = SearchRooms(new LobbySearchFilter
        {
            OnlyAvailable = true,
            OnlyNoPassword = true,
            PropertyFilters = _currentMatchCriteria.CustomParams
        });

        foreach (var room in availableRooms)
        {
            if (room.Properties.TryGetValue("game_mode", out var gameMode) && gameMode == _currentMatchCriteria.GameMode)
            {
                if (room.CurrentPlayerCount >= _currentMatchCriteria.RequiredPlayers)
                {
                    _currentMatchCriteria = null;
                    OnMatchFound?.Invoke(room);
                    return;
                }
            }
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_currentRoom is not null)
        {
            LeaveRoom();
        }

        _rooms.Clear();
        _localMember = null;
    }

    #endregion
}
