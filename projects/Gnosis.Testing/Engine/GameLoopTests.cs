using Gnosis.Core;
using Gnosis.ECS;
using Gnosis.Infrastructure;
using NUnit.Framework;

namespace Gnosis.Testing.Engine;

/// <summary>
/// 游戏主循环测试
/// </summary>
[TestFixture]
public class GameLoopTests
{
    #region 字段

    private ITimeManager _timeManager = null!;
    private ILogger _logger = null!;
    private IGameLoop _gameLoop = null!;

    #endregion

    [SetUp]
    public void SetUp()
    {
        _timeManager = new TimeManager();
        _logger = new ConsoleLogger();
        _gameLoop = new GameLoop(_timeManager, _logger);
    }

    [Test]
    public void Run_ExecutesSystemUpdates()
    {
        var testSystem = new TestSystem(SystemPhase.Update);
        _gameLoop.AddSystem(testSystem);

        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(100);
        _gameLoop.Stop();
        task.Wait(1000);

        Assert.That(testSystem.UpdateCount, Is.GreaterThan(0));
    }

    [Test]
    public void Stop_SetsIsRunningToFalse()
    {
        Assert.That(_gameLoop.IsRunning, Is.False);

        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(50);

        Assert.That(_gameLoop.IsRunning, Is.True);

        _gameLoop.Stop();
        task.Wait(1000);

        Assert.That(_gameLoop.IsRunning, Is.False);
    }

    [Test]
    public void AddSystem_RegistersSystem()
    {
        var testSystem = new TestSystem(SystemPhase.Update);
        _gameLoop.AddSystem(testSystem);

        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(50);
        _gameLoop.Stop();
        task.Wait(1000);

        Assert.That(testSystem.IsInitialized, Is.True);
    }

    [Test]
    public void RemoveSystem_RemovesSystem()
    {
        var testSystem = new TestSystem(SystemPhase.Update);
        _gameLoop.AddSystem(testSystem);
        _gameLoop.RemoveSystem(testSystem);

        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(50);
        _gameLoop.Stop();
        task.Wait(1000);

        Assert.That(testSystem.IsInitialized, Is.False);
        Assert.That(testSystem.UpdateCount, Is.EqualTo(0));
    }

    [Test]
    public void Run_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(50);

        Assert.Throws<InvalidOperationException>(() => _gameLoop.Run());

        _gameLoop.Stop();
        task.Wait(1000);
    }

    [Test]
    public void TargetFrameRate_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _gameLoop.TargetFrameRate = -1);
    }

    [Test]
    public void AddSystem_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _gameLoop.AddSystem(null!));
    }

    [Test]
    public void SystemsAreUpdatedInPhaseOrder()
    {
        var order = new List<string>();
        var initSystem = new OrderedTestSystem(SystemPhase.Initialization, "Init", order);
        var preUpdateSystem = new OrderedTestSystem(SystemPhase.PreUpdate, "PreUpdate", order);
        var updateSystem = new OrderedTestSystem(SystemPhase.Update, "Update", order);
        var postUpdateSystem = new OrderedTestSystem(SystemPhase.PostUpdate, "PostUpdate", order);

        _gameLoop.AddSystem(updateSystem);
        _gameLoop.AddSystem(preUpdateSystem);
        _gameLoop.AddSystem(postUpdateSystem);
        _gameLoop.AddSystem(initSystem);

        var task = Task.Run(() => _gameLoop.Run());
        Thread.Sleep(100);
        _gameLoop.Stop();
        task.Wait(1000);

        Assert.That(order.Count, Is.GreaterThanOrEqualTo(4));
        var initIndex = order.IndexOf("Init");
        var preUpdateIndex = order.IndexOf("PreUpdate");
        var updateIndex = order.IndexOf("Update");
        var postUpdateIndex = order.IndexOf("PostUpdate");

        Assert.That(initIndex, Is.LessThan(preUpdateIndex));
        Assert.That(preUpdateIndex, Is.LessThan(updateIndex));
        Assert.That(updateIndex, Is.LessThan(postUpdateIndex));
    }

    #region 辅助类

    private class TestSystem : ISystem
    {
        public SystemPhase Phase { get; }
        public bool IsInitialized { get; private set; }
        public int UpdateCount { get; private set; }

        public TestSystem(SystemPhase phase)
        {
            Phase = phase;
        }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Update(float delta)
        {
            UpdateCount++;
        }

        public void Shutdown()
        {
        }
    }

    private class OrderedTestSystem : ISystem
    {
        private readonly string _name;
        private readonly List<string> _order;

        public SystemPhase Phase { get; }

        public OrderedTestSystem(SystemPhase phase, string name, List<string> order)
        {
            Phase = phase;
            _name = name;
            _order = order;
        }

        public void Initialize()
        {
        }

        public void Update(float delta)
        {
            _order.Add(_name);
        }

        public void Shutdown()
        {
        }
    }

    #endregion
}
