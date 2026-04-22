using Gnosis.Network.Lobby;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class LobbyServiceTests : GnosisTester
{
    private LobbyService _service = null!;

    public override void Setup()
    {
        base.Setup();
        _service = new LobbyService("local1", "本地玩家");
    }

    public override void Teardown()
    {
        _service.Dispose();
        base.Teardown();
    }

    [Test]
    public void CreateRoom_创建后房间数量增加()
    {
        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });

        Assert.That(_service.RoomCount, Is.EqualTo(1));
    }

    [Test]
    public void CreateRoom_创建后触发OnRoomCreated事件()
    {
        LobbyRoom? createdRoom = null;
        _service.OnRoomCreated += room => createdRoom = room;

        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });

        Assert.That(createdRoom, Is.Not.Null);
    }

    [Test]
    public void JoinRoom_加入后当前房间不为空()
    {
        var room = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });
        _service.JoinRoom(room.RoomId);

        Assert.That(_service.CurrentRoom, Is.Not.Null);
        Assert.That(_service.IsConnected, Is.True);
    }

    [Test]
    public void JoinRoom_加入后触发OnMemberJoined事件()
    {
        var room = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });

        LobbyMember? joinedMember = null;
        _service.OnMemberJoined += (_, member) => joinedMember = member;

        _service.JoinRoom(room.RoomId);

        Assert.That(joinedMember, Is.Not.Null);
    }

    [Test]
    public void LeaveRoom_离开后当前房间为空()
    {
        var room = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });
        _service.JoinRoom(room.RoomId);
        _service.LeaveRoom();

        Assert.That(_service.CurrentRoom, Is.Null);
    }

    [Test]
    public void GetRoom_获取已创建的房间()
    {
        var created = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });

        var room = _service.GetRoom(created.RoomId);
        Assert.That(room, Is.Not.Null);
        Assert.That(room!.RoomId, Is.EqualTo(created.RoomId));
    }

    [Test]
    public void GetRoom_获取不存在的房间返回Null()
    {
        var room = _service.GetRoom("nonexistent");
        Assert.That(room, Is.Null);
    }

    [Test]
    public void DestroyRoom_销毁后房间数量减少()
    {
        var room = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });
        _service.DestroyRoom(room.RoomId);

        Assert.That(_service.RoomCount, Is.EqualTo(0));
    }

    [Test]
    public void DestroyRoom_销毁后触发OnRoomDestroyed事件()
    {
        var room = _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });

        string? destroyedRoomId = null;
        _service.OnRoomDestroyed += id => destroyedRoomId = id;

        _service.DestroyRoom(room.RoomId);

        Assert.That(destroyedRoomId, Is.EqualTo(room.RoomId));
    }

    [Test]
    public void SearchRooms_返回房间列表()
    {
        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });
        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 8 });

        var results = _service.SearchRooms();

        Assert.That(results.Count, Is.EqualTo(2));
    }

    [Test]
    public void IsMatching_开始匹配后为True()
    {
        _service.StartMatching(new MatchCriteria { RequiredPlayers = 2 });

        Assert.That(_service.IsMatching, Is.True);
    }

    [Test]
    public void CancelMatching_取消后不再匹配()
    {
        _service.StartMatching(new MatchCriteria { RequiredPlayers = 2 });
        _service.CancelMatching();

        Assert.That(_service.IsMatching, Is.False);
    }

    [Test]
    public void GetAllRooms_返回所有房间()
    {
        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 4 });
        _service.CreateRoom(new LobbyRoomOptions { MaxPlayers = 8 });

        var rooms = _service.GetAllRooms();

        Assert.That(rooms.Count, Is.EqualTo(2));
    }
}
