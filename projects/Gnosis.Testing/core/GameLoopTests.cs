using Gnosis.ECS.System;
using NUnit.Framework;

namespace Gnosis.core;

[TestFixture]
public class GameLoopTests
{
    private TimeManager _timeManager = null!;
    private ILogger _logger = null!;
    private MockSystemScheduler _scheduler = null!;

    [SetUp]
    public void SetUp()
    {
        _timeManager = new TimeManager();
        _logger = new ConsoleLogger();
        _scheduler = new MockSystemScheduler();
    }

    #region 正常运行

    [Test]
    public void Run_ExecutesSchedulerUpdate()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(100);
        gameLoop.Stop();
        task.Wait(2000);

        Assert.That(_scheduler.UpdateCount, Is.GreaterThan(0));
    }

    [Test]
    public void Run_SetsIsRunningToTrue()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        Assert.That(gameLoop.IsRunning, Is.False);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(50);

        Assert.That(gameLoop.IsRunning, Is.True);

        gameLoop.Stop();
        task.Wait(2000);
    }

    [Test]
    public void Stop_SetsIsRunningToFalse()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(50);

        gameLoop.Stop();
        task.Wait(2000);

        Assert.That(gameLoop.IsRunning, Is.False);
    }

    [Test]
    public void Run_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(50);

        Assert.Throws<InvalidOperationException>(() => gameLoop.Run());

        gameLoop.Stop();
        task.Wait(2000);
    }

    #endregion

    #region 暂停与恢复

    [Test]
    public void Pause_StopsSystemUpdates()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(80);

        gameLoop.Pause();
        Thread.Sleep(80);

        var countAfterPause = _scheduler.UpdateCount;
        Thread.Sleep(80);

        var countAfterPauseWait = _scheduler.UpdateCount;

        gameLoop.Stop();
        task.Wait(2000);

        Assert.That(countAfterPauseWait, Is.EqualTo(countAfterPause));
    }

    [Test]
    public void Resume_RestartsSystemUpdates()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(50);

        gameLoop.Pause();
        Thread.Sleep(50);

        var countAtPause = _scheduler.UpdateCount;
        gameLoop.Resume();
        Thread.Sleep(80);

        gameLoop.Stop();
        task.Wait(2000);

        Assert.That(_scheduler.UpdateCount, Is.GreaterThan(countAtPause));
    }

    [Test]
    public void IsPaused_ReflectsPauseState()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        Assert.That(gameLoop.IsPaused, Is.False);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(50);

        gameLoop.Pause();
        Thread.Sleep(20);

        Assert.That(gameLoop.IsPaused, Is.True);

        gameLoop.Resume();
        Thread.Sleep(20);

        Assert.That(gameLoop.IsPaused, Is.False);

        gameLoop.Stop();
        task.Wait(2000);
    }

    #endregion

    #region 固定时间步更新

    [Test]
    public void Run_ProcessesFixedUpdates()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);

        var task = Task.Run(() => gameLoop.Run());
        Thread.Sleep(150);
        gameLoop.Stop();
        task.Wait(2000);

        Assert.That(_timeManager.FixedFrameCount, Is.GreaterThan(0));
    }

    [Test]
    public void MaxFixedUpdatesPerFrame_DefaultIsFive()
    {
        var gameLoop = new GameLoop(_timeManager, _scheduler, _logger);
        Assert.That(gameLoop.MaxFixedUpdatesPerFrame, Is.EqualTo(5));
    }

    #endregion

    #region 辅助类

    private class MockSystemScheduler : ISystemScheduler
    {
        public int UpdateCount { get; private set; }

        private readonly List<ISystem> _systems = new();

        public void RegisterSystem(ISystem system)
        {
            _systems.Add(system);
        }

        public void UnregisterSystem(ISystem system)
        {
            _systems.Remove(system);
        }

        public void Update(float delta)
        {
            UpdateCount++;
        }

        public void EnableSystem(ISystem system)
        {
        }

        public void DisableSystem(ISystem system)
        {
        }
    }

    #endregion
}
