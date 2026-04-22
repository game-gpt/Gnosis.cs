using System;
using System.Collections.Generic;
using Gnosis.Network.Transport;

namespace Gnosis.Network.Lobby;

/// <summary>
/// 大厅服务接口，提供多人游戏的社交基础设施抽象
/// </summary>
public interface ILobbyService : IDisposable
{
    /// <summary>
    /// 创建房间
    /// </summary>
    /// <param name="options">房间创建选项</param>
    /// <returns>创建的房间信息</returns>
    LobbyRoom CreateRoom(LobbyRoomOptions options);

    /// <summary>
    /// 加入指定房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="password">房间密码（可选）</param>
    /// <returns>加入的房间信息</returns>
    LobbyRoom JoinRoom(string roomId, string? password = null);

    /// <summary>
    /// 离开当前房间
    /// </summary>
    void LeaveRoom();

    /// <summary>
    /// 搜索房间
    /// </summary>
    /// <param name="filter">搜索过滤条件</param>
    /// <returns>匹配的房间列表</returns>
    IReadOnlyList<LobbyRoom> SearchRooms(LobbySearchFilter? filter = null);

    /// <summary>
    /// 获取当前所在的房间
    /// </summary>
    LobbyRoom? CurrentRoom { get; }

    /// <summary>
    /// 获取当前房间的所有成员
    /// </summary>
    IReadOnlyList<LobbyMember> Members { get; }

    /// <summary>
    /// 是否已连接到大厅服务
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 房间创建时触发
    /// </summary>
    event Action<LobbyRoom>? OnRoomCreated;

    /// <summary>
    /// 加入房间时触发
    /// </summary>
    event Action<LobbyRoom, LobbyMember>? OnMemberJoined;

    /// <summary>
    /// 离开房间时触发
    /// </summary>
    event Action<LobbyRoom, LobbyMember>? OnMemberLeft;

    /// <summary>
    /// 房间销毁时触发
    /// </summary>
    event Action<string>? OnRoomDestroyed;

    /// <summary>
    /// 房间属性更新时触发
    /// </summary>
    event Action<LobbyRoom>? OnRoomUpdated;
}

/// <summary>
/// 匹配接口，提供玩家匹配功能
/// </summary>
public interface IMatchmaker
{
    /// <summary>
    /// 加入匹配队列
    /// </summary>
    /// <param name="criteria">匹配条件</param>
    void StartMatching(MatchCriteria criteria);

    /// <summary>
    /// 取消匹配
    /// </summary>
    void CancelMatching();

    /// <summary>
    /// 是否正在匹配中
    /// </summary>
    bool IsMatching { get; }

    /// <summary>
    /// 匹配成功时触发
    /// </summary>
    event Action<LobbyRoom>? OnMatchFound;

    /// <summary>
    /// 匹配超时时触发
    /// </summary>
    event Action? OnMatchTimeout;
}

/// <summary>
/// 房间管理接口，提供房间的增删改查操作
/// </summary>
public interface IRoomManager
{
    /// <summary>
    /// 获取所有房间
    /// </summary>
    IReadOnlyList<LobbyRoom> GetAllRooms();

    /// <summary>
    /// 获取指定房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <returns>房间信息，不存在则返回 null</returns>
    LobbyRoom? GetRoom(string roomId);

    /// <summary>
    /// 更新房间信息
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="updates">更新数据</param>
    void UpdateRoom(string roomId, Dictionary<string, string> updates);

    /// <summary>
    /// 销毁房间
    /// </summary>
    /// <param name="roomId">房间标识</param>
    void DestroyRoom(string roomId);

    /// <summary>
    /// 踢出房间成员
    /// </summary>
    /// <param name="roomId">房间标识</param>
    /// <param name="memberId">成员标识</param>
    void KickMember(string roomId, string memberId);
}
