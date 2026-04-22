using System;
using System.Collections.Generic;

namespace Gnosis.Network.Lobby;

/// <summary>
/// 大厅房间信息
/// </summary>
public sealed class LobbyRoom
{
    #region 属性

    /// <summary>
    /// 房间标识
    /// </summary>
    public string RoomId { get; init; }

    /// <summary>
    /// 房间名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 房主标识
    /// </summary>
    public string HostId { get; set; }

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; init; }

    /// <summary>
    /// 当前玩家数
    /// </summary>
    public int CurrentPlayerCount => Members.Count;

    /// <summary>
    /// 是否有密码保护
    /// </summary>
    public bool HasPassword { get; init; }

    /// <summary>
    /// 房间自定义属性
    /// </summary>
    public Dictionary<string, string> Properties { get; init; }

    /// <summary>
    /// 房间成员列表
    /// </summary>
    public List<LobbyMember> Members { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 是否已满
    /// </summary>
    public bool IsFull => CurrentPlayerCount >= MaxPlayers;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化大厅房间
    /// </summary>
    public LobbyRoom(string roomId, string name, string hostId, int maxPlayers, bool hasPassword = false)
    {
        RoomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        HostId = hostId ?? throw new ArgumentNullException(nameof(hostId));
        MaxPlayers = maxPlayers;
        HasPassword = hasPassword;
        Properties = new Dictionary<string, string>();
        Members = new List<LobbyMember>();
        CreatedAt = DateTime.UtcNow;
    }

    #endregion
}

/// <summary>
/// 大厅房间成员
/// </summary>
public sealed class LobbyMember
{
    #region 属性

    /// <summary>
    /// 成员标识
    /// </summary>
    public string MemberId { get; init; }

    /// <summary>
    /// 成员名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 是否为房主
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; init; }

    /// <summary>
    /// 成员自定义属性
    /// </summary>
    public Dictionary<string, string> Properties { get; init; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化大厅成员
    /// </summary>
    public LobbyMember(string memberId, string name, bool isHost = false)
    {
        MemberId = memberId ?? throw new ArgumentNullException(nameof(memberId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsHost = isHost;
        JoinedAt = DateTime.UtcNow;
        Properties = new Dictionary<string, string>();
    }

    #endregion
}

/// <summary>
/// 房间创建选项
/// </summary>
public sealed class LobbyRoomOptions
{
    /// <summary>
    /// 房间名称
    /// </summary>
    public string Name { get; set; } = "未命名房间";

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; set; } = 16;

    /// <summary>
    /// 房间密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>
/// 房间搜索过滤条件
/// </summary>
public sealed class LobbySearchFilter
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 最大玩家数下限
    /// </summary>
    public int? MinMaxPlayers { get; set; }

    /// <summary>
    /// 最大玩家数上限
    /// </summary>
    public int? MaxMaxPlayers { get; set; }

    /// <summary>
    /// 是否只显示未满的房间
    /// </summary>
    public bool OnlyAvailable { get; set; } = true;

    /// <summary>
    /// 是否只显示无密码的房间
    /// </summary>
    public bool OnlyNoPassword { get; set; }

    /// <summary>
    /// 属性过滤条件
    /// </summary>
    public Dictionary<string, string>? PropertyFilters { get; set; }
}

/// <summary>
/// 匹配条件
/// </summary>
public sealed class MatchCriteria
{
    /// <summary>
    /// 匹配模式标识
    /// </summary>
    public string GameMode { get; set; } = "default";

    /// <summary>
    /// 需要的玩家数量
    /// </summary>
    public int RequiredPlayers { get; set; } = 2;

    /// <summary>
    /// 匹配超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 技能等级范围（最小值）
    /// </summary>
    public int MinSkillLevel { get; set; }

    /// <summary>
    /// 技能等级范围（最大值）
    /// </summary>
    public int MaxSkillLevel { get; set; } = 100;

    /// <summary>
    /// 自定义匹配参数
    /// </summary>
    public Dictionary<string, string>? CustomParams { get; set; }
}
