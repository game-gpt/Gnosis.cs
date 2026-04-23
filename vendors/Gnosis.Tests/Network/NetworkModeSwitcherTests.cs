using Gnosis.Network.Channel;
using Gnosis.Network.Prediction;
using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Network;

public class NetworkModeSwitcherTests : GnosisTester
{
    private NetworkModeSwitcher _switcher = null!;
    private NetworkManager _networkManager = null!;

    public override void Setup()
    {
        base.Setup();
        _networkManager = new NetworkManager(NetworkBackendType.None);
        _switcher = new NetworkModeSwitcher(_networkManager);
    }

    public override void Teardown()
    {
        _networkManager.Shutdown();
        base.Teardown();
    }

    [Test]
    public void CurrentMode_初始为None()
    {
        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.None));
    }

    [Test]
    public void SwitchTo_切换到状态同步模式()
    {
        _switcher.SwitchTo(SyncMode.StateSync);

        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.StateSync));
    }

    [Test]
    public void SwitchTo_切换到帧同步模式()
    {
        _switcher.SwitchTo(SyncMode.Lockstep);

        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.Lockstep));
    }

    [Test]
    public void SwitchTo_相同模式不触发事件()
    {
        int eventCount = 0;
        _switcher.OnModeChanged += _ => eventCount++;

        _switcher.SwitchTo(SyncMode.None);

        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void SwitchTo_触发OnModeChanging事件()
    {
        SyncMode? fromMode = null;
        SyncMode? toMode = null;
        _switcher.OnModeChanging += (from, to) =>
        {
            fromMode = from;
            toMode = to;
        };

        _switcher.SwitchTo(SyncMode.StateSync);

        Assert.That(fromMode, Is.EqualTo(SyncMode.None));
        Assert.That(toMode, Is.EqualTo(SyncMode.StateSync));
    }

    [Test]
    public void SwitchTo_触发OnModeChanged事件()
    {
        SyncMode? newMode = null;
        _switcher.OnModeChanged += mode => newMode = mode;

        _switcher.SwitchTo(SyncMode.Lockstep);

        Assert.That(newMode, Is.EqualTo(SyncMode.Lockstep));
    }

    [Test]
    public void SwitchToStateSync_便捷方法()
    {
        _switcher.SwitchToStateSync();

        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.StateSync));
    }

    [Test]
    public void SwitchToLockstep_便捷方法()
    {
        _switcher.SwitchToLockstep();

        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.Lockstep));
    }

    [Test]
    public void SwitchToOffline_便捷方法()
    {
        _switcher.SwitchToStateSync();
        _switcher.SwitchToOffline();

        Assert.That(_switcher.CurrentMode, Is.EqualTo(SyncMode.None));
    }

    [Test]
    public void SwitchTo_状态同步模式启用预测()
    {
        var stateSync = new StateSyncSystem(_networkManager, new MessageSerializer());
        _switcher.StateSyncSystem = stateSync;

        _switcher.SwitchTo(SyncMode.StateSync);

        Assert.That(stateSync.PredictionEnabled, Is.True);
    }

    [Test]
    public void SwitchTo_从状态同步切走时禁用预测()
    {
        var stateSync = new StateSyncSystem(_networkManager, new MessageSerializer());
        _switcher.StateSyncSystem = stateSync;

        _switcher.SwitchTo(SyncMode.StateSync);
        _switcher.SwitchTo(SyncMode.None);

        Assert.That(stateSync.PredictionEnabled, Is.False);
    }
}