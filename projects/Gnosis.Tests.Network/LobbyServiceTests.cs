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
        _service = new LobbyService();
    }

    public override void Teardown()
    {
        _service.Dispose();
        base.Teardown();
    }

    [Test]
    public void CreateRoom_创建后房间数量增加()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });

        Assert.That(_service.RoomCount, Is.EqualTo(1));
    }

    [Test]
    public void CreateRoom_创建后触发OnRoomCreated事件()
    {
        LobbyRoom? createdRoom = null;
        _service.OnRoomCreated += room => createdRoom = room;

        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });

        Assert.That(createdRoom, Is.Not.Null);
        Assert.That(createdRoom!.Id, Is.EqualTo("room1"));
    }

    [Test]
    public void JoinRoom_加入后成员数量增加()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.JoinRoom("room1", "player1");

        Assert.That(_service.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public void JoinRoom_加入后触发OnMemberJoined事件()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });

        LobbyMember? joinedMember = null;
        _service.OnMemberJoined += (_, member) => joinedMember = member;

        _service.JoinRoom("room1", "player1");

        Assert.That(joinedMember, Is.Not.Null);
        Assert.That(joinedMember!.Id, Is.EqualTo("player1"));
    }

    [Test]
    public void LeaveRoom_离开后成员数量减少()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.JoinRoom("room1", "player1");
        _service.LeaveRoom("room1", "player1");

        Assert.That(_service.Members.Count, Is.EqualTo(0));
    }

    [Test]
    public void LeaveRoom_房主离开后房主迁移()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.JoinRoom("room1", "player1");
        _service.JoinRoom("room1", "player2");
        _service.LeaveRoom("room1", "player1");

        Assert.That(_service.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetRoom_获取已创建的房间()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });

        var room = _service.GetRoom("room1");
        Assert.That(room, Is.Not.Null);
        Assert.That(room!.Id, Is.EqualTo("room1"));
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
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.DestroyRoom("room1");

        Assert.That(_service.RoomCount, Is.EqualTo(0));
    }

    [Test]
    public void DestroyRoom_销毁后触发OnRoomDestroyed事件()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });

        string? destroyedRoomId = null;
        _service.OnRoomDestroyed += id => destroyedRoomId = id;

        _service.DestroyRoom("room1");

        Assert.That(destroyedRoomId, Is.EqualTo("room1"));
    }

    [Test]
    public void SearchRooms_按条件搜索房间()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.CreateRoom("room2", new LobbyRoomOptions { MaxMembers = 8 });

        var filter = new LobbySearchFilter { MaxMemberCount = 4 };
        var results = _service.SearchRooms(filter);

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo("room1"));
    }

    [Test]
    public void IsConnected_加入房间后为True()
    {
        _service.CreateRoom("room1", new LobbyRoomOptions { MaxMembers = 4 });
        _service.JoinRoom("room1", "player1");

        Assert.That(_service.IsConnected, Is.True);
    }

    [Test]
    public void IsMatching_开始匹配后为True()
    {
        _service.StartMatchmaking(new MatchCriteria { MaxTeamSize = 2 });

        Assert.That(_service.IsMatching, Is.True);
    }

    [Test]
    public void CancelMatchmaking_取消后不再匹配()
    {
        _service.StartMatchmaking(new MatchCriteria { MaxTeamSize = 2 });
        _service.CancelMatchmaking();

        Assert.That(_service.IsMatching, Is.False);
    }
}
