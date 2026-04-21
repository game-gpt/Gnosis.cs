using Gnosis.Assets.VFS;
using Gnosis.Core;
using Gnosis.Infrastructure;
using NUnit.Framework;

namespace Gnosis.Engine.Testing;

/// <summary>
/// Gnosis 引擎入口测试
/// </summary>
[TestFixture]
public class GnosisEngineTester
{
    #region 字段

    private ILogger _logger = null!;
    private ITimeManager _timeManager = null!;
    private IGameLoop _gameLoop = null!;

    #endregion

    [SetUp]
    public void SetUp()
    {
        _logger = new ConsoleLogger();
        _timeManager = new TimeManager();
        _gameLoop = new GameLoop(_timeManager, _logger);
    }

    [Test]
    public void Initialize_SetsIsInitializedToTrue()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);

        engine.Initialize();

        Assert.That(engine.IsInitialized, Is.True);
    }

    [Test]
    public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperationException()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);
        engine.Initialize();

        Assert.Throws<InvalidOperationException>(() => engine.Initialize());
    }

    [Test]
    public void Run_WithoutInitialize_ThrowsInvalidOperationException()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);

        Assert.Throws<InvalidOperationException>(() => engine.Run());
    }

    [Test]
    public void Run_StartsGameLoop()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);
        engine.Initialize();

        var task = Task.Run(() => engine.Run());
        Thread.Sleep(50);

        Assert.That(engine.IsRunning, Is.True);

        engine.Shutdown();
        task.Wait(1000);
    }

    [Test]
    public void Shutdown_StopsEngine()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);
        engine.Initialize();

        var task = Task.Run(() => engine.Run());
        Thread.Sleep(50);

        engine.Shutdown();
        task.Wait(1000);

        Assert.That(engine.IsInitialized, Is.False);
        Assert.That(engine.IsRunning, Is.False);
    }

    [Test]
    public void Shutdown_WhenNotInitialized_DoesNotThrow()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);

        Assert.DoesNotThrow(() => engine.Shutdown());
    }

    [Test]
    public void AddSystem_RegistersSystem()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);
        engine.Initialize();

        var testSystem = new TestSystem();
        engine.AddSystem(testSystem);

        var task = Task.Run(() => engine.Run());
        Thread.Sleep(50);
        engine.Shutdown();
        task.Wait(1000);

        Assert.That(testSystem.IsInitialized, Is.True);
    }

    [Test]
    public void SetConfigSystem_SetsConfigSystem()
    {
        using var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);

        var mockVfs = new MockVirtualFileSystem();
        var configSystem = new ConfigSystem(mockVfs);
        engine.SetConfigSystem(configSystem);

        Assert.That(engine.ConfigSystem, Is.SameAs(configSystem));
    }

    [Test]
    public void Dispose_CallsShutdown()
    {
        var engine = new GnosisEngine(_logger, _timeManager, _gameLoop);
        engine.Initialize();

        engine.Dispose();

        Assert.That(engine.IsInitialized, Is.False);
    }

    #region 辅助类

    private class TestSystem : ISystem
    {
        public Gnosis.ECS.SystemPhase Phase => Gnosis.ECS.SystemPhase.Update;
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Update(float delta)
        {
        }

        public void Shutdown()
        {
        }
    }

    private class MockVirtualFileSystem : IVirtualFileSystem
    {
        public void Mount(string path, IFileSystem fileSystem) { }
        public void Unmount(string path) { }
        public Stream? OpenRead(string path) => null;
        public Stream? OpenWrite(string path) => new MemoryStream();
        public bool FileExists(string path) => false;
        public bool DirectoryExists(string path) => false;
        public void CreateDirectory(string path) { }
        public void DeleteFile(string path) { }
        public IEnumerable<string> GetFiles(string path, string searchPattern = "*") => [];
        public IEnumerable<string> GetDirectories(string path) => [];
    }

    #endregion
}
