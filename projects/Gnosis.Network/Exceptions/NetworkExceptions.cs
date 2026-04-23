using Gnosis.Core.Exceptions;

namespace Gnosis.Network.Exceptions;

/// <summary>
///     网络未连接异常，当尝试在未连接状态下发送消息时抛出
/// </summary>
public sealed class NetworkNotConnectedException : GnosisException
{
    public NetworkNotConnectedException()
        : base("NET_001", "网络未连接，无法发送消息")
    {
    }
}

/// <summary>
///     网络连接失败异常
/// </summary>
public sealed class NetworkConnectionFailedException : GnosisException
{
    public NetworkConnectionFailedException(string reason)
        : base("NET_002", $"网络连接失败: {reason}")
    {
    }
}

/// <summary>
///     网络超时异常
/// </summary>
public sealed class NetworkTimeoutException : GnosisException
{
    public NetworkTimeoutException(string operation)
        : base("NET_003", $"网络操作超时: {operation}")
    {
    }
}

/// <summary>
///     大厅服务异常基类
/// </summary>
public abstract class LobbyException : GnosisException
{
    protected LobbyException(string errorCode, string message)
        : base(errorCode, message)
    {
    }
}

/// <summary>
///     大厅未连接异常
/// </summary>
public sealed class LobbyNotConnectedException : LobbyException
{
    public LobbyNotConnectedException()
        : base("LOBBY_001", "大厅服务未连接")
    {
    }
}

/// <summary>
///     大厅房间不存在异常
/// </summary>
public sealed class LobbyRoomNotFoundException : LobbyException
{
    public LobbyRoomNotFoundException(string roomId)
        : base("LOBBY_002", $"房间 {roomId} 不存在")
    {
    }
}

/// <summary>
///     大厅房间已满异常
/// </summary>
public sealed class LobbyRoomFullException : LobbyException
{
    public LobbyRoomFullException(string roomId)
        : base("LOBBY_003", $"房间 {roomId} 已满")
    {
    }
}

/// <summary>
///     大厅已在房间中异常
/// </summary>
public sealed class LobbyAlreadyInRoomException : LobbyException
{
    public LobbyAlreadyInRoomException()
        : base("LOBBY_004", "已在房间中，请先离开当前房间")
    {
    }
}
