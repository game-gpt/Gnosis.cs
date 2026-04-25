namespace Gnosis.Network.Lobby;

public sealed class LobbyStateSync
{
    #region 字段

    private readonly Dictionary<string, LobbyRoomState> _roomStates = new();
    private readonly List<LobbyStateChange> _pendingChanges = new();
    private readonly LobbyService _lobbyService;
    private int _stateVersion;

    #endregion

    #region 属性

    public int StateVersion => _stateVersion;

    public int PendingChangeCount => _pendingChanges.Count;

    #endregion

    #region 事件

    public event Action<LobbyStateChange>? OnStateChanged;

    public event Action<int, byte[]>? OnStateSyncRequired;

    #endregion

    #region 构造函数

    public LobbyStateSync(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;

        _lobbyService.OnRoomCreated += HandleRoomCreated;
        _lobbyService.OnMemberJoined += HandleMemberJoined;
        _lobbyService.OnMemberLeft += HandleMemberLeft;
        _lobbyService.OnRoomDestroyed += HandleRoomDestroyed;
        _lobbyService.OnRoomUpdated += HandleRoomUpdated;
    }

    #endregion

    #region 公开方法

    public void Update()
    {
        if (_pendingChanges.Count == 0)
        {
            return;
        }

        foreach (var change in _pendingChanges)
        {
            OnStateChanged?.Invoke(change);
        }

        _pendingChanges.Clear();
    }

    public byte[] SerializeState()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(_stateVersion);
        writer.Write(_roomStates.Count);

        foreach (var (roomId, state) in _roomStates)
        {
            writer.Write(roomId);
            writer.Write(state.Name);
            writer.Write(state.HostId);
            writer.Write(state.MaxPlayers);
            writer.Write(state.CurrentPlayerCount);
            writer.Write(state.HasPassword);
            writer.Write(state.Properties.Count);

            foreach (var (key, value) in state.Properties)
            {
                writer.Write(key);
                writer.Write(value);
            }

            writer.Write(state.MemberIds.Count);
            foreach (var memberId in state.MemberIds)
            {
                writer.Write(memberId);
            }
        }

        return stream.ToArray();
    }

    public void DeserializeState(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        _stateVersion = reader.ReadInt32();
        var roomCount = reader.ReadInt32();

        _roomStates.Clear();

        for (int i = 0; i < roomCount; i++)
        {
            var roomId = reader.ReadString();
            var name = reader.ReadString();
            var hostId = reader.ReadString();
            var maxPlayers = reader.ReadInt32();
            var currentPlayerCount = reader.ReadInt32();
            var hasPassword = reader.ReadBoolean();
            var propCount = reader.ReadInt32();

            var properties = new Dictionary<string, string>();
            for (int j = 0; j < propCount; j++)
            {
                properties[reader.ReadString()] = reader.ReadString();
            }

            var memberCount = reader.ReadInt32();
            var memberIds = new List<string>();
            for (int j = 0; j < memberCount; j++)
            {
                memberIds.Add(reader.ReadString());
            }

            _roomStates[roomId] = new LobbyRoomState
            {
                RoomId = roomId,
                Name = name,
                HostId = hostId,
                MaxPlayers = maxPlayers,
                CurrentPlayerCount = currentPlayerCount,
                HasPassword = hasPassword,
                Properties = properties,
                MemberIds = memberIds
            };
        }
    }

    public LobbyRoomState? GetRoomState(string roomId)
    {
        return _roomStates.GetValueOrDefault(roomId);
    }

    public IReadOnlyList<LobbyStateChange> GetPendingChanges()
    {
        return _pendingChanges.AsReadOnly();
    }

    #endregion

    #region 私有方法 - 事件处理

    private void HandleRoomCreated(LobbyRoom room)
    {
        _stateVersion++;

        var state = RoomToState(room);
        _roomStates[room.RoomId] = state;

        _pendingChanges.Add(new LobbyStateChange
        {
            ChangeType = LobbyStateChangeType.RoomCreated,
            RoomId = room.RoomId,
            StateVersion = _stateVersion,
            Data = SerializeRoomState(state)
        });
    }

    private void HandleMemberJoined(LobbyRoom room, LobbyMember member)
    {
        _stateVersion++;

        var state = RoomToState(room);
        _roomStates[room.RoomId] = state;

        _pendingChanges.Add(new LobbyStateChange
        {
            ChangeType = LobbyStateChangeType.MemberJoined,
            RoomId = room.RoomId,
            StateVersion = _stateVersion,
            MemberId = member.MemberId,
            Data = SerializeRoomState(state)
        });
    }

    private void HandleMemberLeft(LobbyRoom room, LobbyMember member)
    {
        _stateVersion++;

        var state = RoomToState(room);
        _roomStates[room.RoomId] = state;

        _pendingChanges.Add(new LobbyStateChange
        {
            ChangeType = LobbyStateChangeType.MemberLeft,
            RoomId = room.RoomId,
            StateVersion = _stateVersion,
            MemberId = member.MemberId,
            Data = SerializeRoomState(state)
        });
    }

    private void HandleRoomDestroyed(string roomId)
    {
        _stateVersion++;

        _roomStates.Remove(roomId);

        _pendingChanges.Add(new LobbyStateChange
        {
            ChangeType = LobbyStateChangeType.RoomDestroyed,
            RoomId = roomId,
            StateVersion = _stateVersion
        });
    }

    private void HandleRoomUpdated(LobbyRoom room)
    {
        _stateVersion++;

        var state = RoomToState(room);
        _roomStates[room.RoomId] = state;

        _pendingChanges.Add(new LobbyStateChange
        {
            ChangeType = LobbyStateChangeType.RoomUpdated,
            RoomId = room.RoomId,
            StateVersion = _stateVersion,
            Data = SerializeRoomState(state)
        });
    }

    #endregion

    #region 私有方法 - 序列化

    private static LobbyRoomState RoomToState(LobbyRoom room)
    {
        return new LobbyRoomState
        {
            RoomId = room.RoomId,
            Name = room.Name,
            HostId = room.HostId,
            MaxPlayers = room.MaxPlayers,
            CurrentPlayerCount = room.CurrentPlayerCount,
            HasPassword = room.HasPassword,
            Properties = new Dictionary<string, string>(room.Properties),
            MemberIds = room.Members.Select(m => m.MemberId).ToList()
        };
    }

    private static byte[] SerializeRoomState(LobbyRoomState state)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(state.RoomId);
        writer.Write(state.Name);
        writer.Write(state.HostId);
        writer.Write(state.MaxPlayers);
        writer.Write(state.CurrentPlayerCount);
        writer.Write(state.HasPassword);
        writer.Write(state.Properties.Count);

        foreach (var (key, value) in state.Properties)
        {
            writer.Write(key);
            writer.Write(value);
        }

        writer.Write(state.MemberIds.Count);
        foreach (var memberId in state.MemberIds)
        {
            writer.Write(memberId);
        }

        return stream.ToArray();
    }

    #endregion
}

public sealed class LobbyRoomState
{
    public string RoomId { get; init; } = "";
    public string Name { get; set; } = "";
    public string HostId { get; set; } = "";
    public int MaxPlayers { get; init; }
    public int CurrentPlayerCount { get; set; }
    public bool HasPassword { get; init; }
    public Dictionary<string, string> Properties { get; init; } = new();
    public List<string> MemberIds { get; init; } = new();
}

public sealed class LobbyStateChange
{
    public LobbyStateChangeType ChangeType { get; init; }
    public string RoomId { get; init; } = "";
    public int StateVersion { get; init; }
    public string? MemberId { get; init; }
    public byte[]? Data { get; init; }
}

public enum LobbyStateChangeType
{
    RoomCreated,
    MemberJoined,
    MemberLeft,
    RoomDestroyed,
    RoomUpdated
}
