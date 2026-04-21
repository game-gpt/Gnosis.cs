using Gnosis.Assets.Formats;
using NUnit.Framework;

namespace Gnosis.Testing.Assets.Formats;

[TestFixture]
public class FormatRegistryTests : TestBase
{
    private FormatRegistry _registry = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _registry = new FormatRegistry();
    }

    #region 基础功能测试

    [Test]
    public void RegisterHandler_添加处理器_可通过类型获取()
    {
        var handler = new MockFormatHandler(FormatType.Texture, ".png");

        _registry.RegisterHandler(handler);

        var result = _registry.GetHandler(FormatType.Texture);
        Assert.That(result, Is.SameAs(handler));
    }

    [Test]
    public void RegisterHandler_重复注册同一类型_替换旧处理器()
    {
        var handler1 = new MockFormatHandler(FormatType.Texture, ".png");
        var handler2 = new MockFormatHandler(FormatType.Texture, ".jpg");

        _registry.RegisterHandler(handler1);
        _registry.RegisterHandler(handler2);

        var result = _registry.GetHandler(FormatType.Texture);
        Assert.That(result, Is.SameAs(handler2));
    }

    [Test]
    public void UnregisterHandler_移除处理器_无法再获取()
    {
        var handler = new MockFormatHandler(FormatType.Mesh, ".obj");
        _registry.RegisterHandler(handler);

        _registry.UnregisterHandler(FormatType.Mesh);

        var result = _registry.GetHandler(FormatType.Mesh);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void UnregisterHandler_移除不存在的类型_不抛异常()
    {
        Assert.DoesNotThrow(() => _registry.UnregisterHandler(FormatType.Audio));
    }

    [Test]
    public void GetHandler_按类型查找_返回正确处理器()
    {
        var textureHandler = new MockFormatHandler(FormatType.Texture, ".png");
        var meshHandler = new MockFormatHandler(FormatType.Mesh, ".obj");

        _registry.RegisterHandler(textureHandler);
        _registry.RegisterHandler(meshHandler);

        Assert.That(_registry.GetHandler(FormatType.Texture), Is.SameAs(textureHandler));
        Assert.That(_registry.GetHandler(FormatType.Mesh), Is.SameAs(meshHandler));
    }

    [Test]
    public void GetHandler_按路径查找_返回匹配处理器()
    {
        var textureHandler = new MockFormatHandler(FormatType.Texture, ".png");
        var meshHandler = new MockFormatHandler(FormatType.Mesh, ".obj");

        _registry.RegisterHandler(textureHandler);
        _registry.RegisterHandler(meshHandler);

        Assert.That(_registry.GetHandler("image.png"), Is.SameAs(textureHandler));
        Assert.That(_registry.GetHandler("model.obj"), Is.SameAs(meshHandler));
    }

    [Test]
    public void GetHandler_按路径查找无匹配_返回null()
    {
        var handler = new MockFormatHandler(FormatType.Texture, ".png");
        _registry.RegisterHandler(handler);

        var result = _registry.GetHandler("model.obj");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAllHandlers_返回所有已注册处理器()
    {
        var textureHandler = new MockFormatHandler(FormatType.Texture, ".png");
        var meshHandler = new MockFormatHandler(FormatType.Mesh, ".obj");
        var audioHandler = new MockFormatHandler(FormatType.Audio, ".wav");

        _registry.RegisterHandler(textureHandler);
        _registry.RegisterHandler(meshHandler);
        _registry.RegisterHandler(audioHandler);

        var allHandlers = _registry.GetAllHandlers().ToList();
        Assert.That(allHandlers.Count, Is.EqualTo(3));
        Assert.That(allHandlers, Does.Contain(textureHandler));
        Assert.That(allHandlers, Does.Contain(meshHandler));
        Assert.That(allHandlers, Does.Contain(audioHandler));
    }

    [Test]
    public void HasHandler_已注册返回true()
    {
        var handler = new MockFormatHandler(FormatType.Texture, ".png");
        _registry.RegisterHandler(handler);

        Assert.That(_registry.HasHandler(FormatType.Texture), Is.True);
    }

    [Test]
    public void HasHandler_未注册返回false()
    {
        Assert.That(_registry.HasHandler(FormatType.Texture), Is.False);
    }

    [Test]
    public void UnregisterHandler_从列表中移除处理器()
    {
        var handler1 = new MockFormatHandler(FormatType.Texture, ".png");
        var handler2 = new MockFormatHandler(FormatType.Mesh, ".obj");

        _registry.RegisterHandler(handler1);
        _registry.RegisterHandler(handler2);
        _registry.UnregisterHandler(FormatType.Texture);

        var allHandlers = _registry.GetAllHandlers().ToList();
        Assert.That(allHandlers.Count, Is.EqualTo(1));
        Assert.That(allHandlers[0], Is.SameAs(handler2));
    }

    [Test]
    public void RegisterHandler_替换处理器后列表数量不变()
    {
        var handler1 = new MockFormatHandler(FormatType.Texture, ".png");
        var handler2 = new MockFormatHandler(FormatType.Texture, ".jpg");

        _registry.RegisterHandler(handler1);
        _registry.RegisterHandler(handler2);

        var allHandlers = _registry.GetAllHandlers().ToList();
        Assert.That(allHandlers.Count, Is.EqualTo(1));
        Assert.That(allHandlers[0], Is.SameAs(handler2));
    }

    #endregion

    #region 并发测试

    [Test]
    public void ConcurrentRegisterHandler_多线程注册_所有处理器均可用()
    {
        const int threadCount = 8;
        const int handlersPerThread = 100;
        var formatTypes = new[]
        {
            FormatType.Texture, FormatType.Mesh, FormatType.Audio,
            FormatType.Shader, FormatType.Animation, FormatType.Config,
            FormatType.Material, FormatType.Scene
        };

        var tasks = new Task[threadCount];
        var barrier = new Barrier(threadCount);

        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            tasks[t] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (var i = 0; i < handlersPerThread; i++)
                {
                    var formatType = formatTypes[(threadIndex * handlersPerThread + i) % formatTypes.Length];
                    var extension = $".{formatType.ToString().ToLowerInvariant()}";
                    var handler = new MockFormatHandler(formatType, extension);
                    _registry.RegisterHandler(handler);
                }
            });
        }

        Task.WaitAll(tasks);

        foreach (var formatType in formatTypes)
        {
            Assert.That(_registry.HasHandler(formatType), Is.True,
                $"并发注册后应包含 {formatType} 类型的处理器");
            Assert.That(_registry.GetHandler(formatType), Is.Not.Null,
                $"并发注册后应能获取 {formatType} 类型的处理器");
        }
    }

    [Test]
    public void ConcurrentGetHandlerWhileRegistering_读写并发_不抛异常()
    {
        const int readerCount = 4;
        const int writerCount = 4;
        const int iterations = 200;
        var formatTypes = new[]
        {
            FormatType.Texture, FormatType.Mesh, FormatType.Audio,
            FormatType.Shader, FormatType.Animation, FormatType.Config,
            FormatType.Material, FormatType.Scene, FormatType.Prefab,
            FormatType.Asset, FormatType.Script, FormatType.Localization
        };

        var barrier = new Barrier(readerCount + writerCount);
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        for (var t = 0; t < writerCount; t++)
        {
            var threadIndex = t;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (var i = 0; i < iterations; i++)
                    {
                        var formatType = formatTypes[(threadIndex * iterations + i) % formatTypes.Length];
                        var extension = $".{formatType.ToString().ToLowerInvariant()}";
                        var handler = new MockFormatHandler(formatType, extension);
                        _registry.RegisterHandler(handler);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        for (var t = 0; t < readerCount; t++)
        {
            var threadIndex = t;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (var i = 0; i < iterations; i++)
                    {
                        var formatType = formatTypes[(threadIndex * iterations + i) % formatTypes.Length];
                        _registry.GetHandler(formatType);
                        _registry.HasHandler(formatType);
                        _registry.GetAllHandlers();
                        var extension = $".{formatType.ToString().ToLowerInvariant()}";
                        _registry.GetHandler($"test{extension}");
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty,
            $"并发读写期间不应抛出异常，但捕获到 {exceptions.Count} 个异常");
    }

    [Test]
    public void ConcurrentRegisterAndUnregister_注册与注销并发_不抛异常()
    {
        const int threadCount = 4;
        const int iterations = 200;
        var formatTypes = new[]
        {
            FormatType.Texture, FormatType.Mesh, FormatType.Audio,
            FormatType.Shader, FormatType.Animation, FormatType.Config
        };

        var barrier = new Barrier(threadCount * 2);
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (var i = 0; i < iterations; i++)
                    {
                        var formatType = formatTypes[(threadIndex * iterations + i) % formatTypes.Length];
                        var extension = $".{formatType.ToString().ToLowerInvariant()}";
                        var handler = new MockFormatHandler(formatType, extension);
                        _registry.RegisterHandler(handler);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (var i = 0; i < iterations; i++)
                    {
                        var formatType = formatTypes[(threadIndex * iterations + i) % formatTypes.Length];
                        _registry.UnregisterHandler(formatType);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty,
            $"并发注册与注销期间不应抛出异常，但捕获到 {exceptions.Count} 个异常");
    }

    [Test]
    public void ConcurrentGetAllHandlers_迭代期间注册新处理器_不抛异常()
    {
        const int iterations = 100;
        var exceptions = new List<Exception>();

        for (var i = 0; i < 10; i++)
        {
            var handler = new MockFormatHandler(FormatType.Texture + i, $".ext{i}");
            _registry.RegisterHandler(handler);
        }

        var barrier = new Barrier(2);
        var tasks = new List<Task>();

        tasks.Add(Task.Run(() =>
        {
            try
            {
                barrier.SignalAndWait();

                for (var i = 0; i < iterations; i++)
                {
                    foreach (var handler in _registry.GetAllHandlers())
                    {
                        _ = handler.SupportedFormat;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        }));

        tasks.Add(Task.Run(() =>
        {
            try
            {
                barrier.SignalAndWait();

                for (var i = 0; i < iterations; i++)
                {
                    var handler = new MockFormatHandler(FormatType.Prefab, $".prefab{i}");
                    _registry.RegisterHandler(handler);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        }));

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty,
            $"迭代期间注册新处理器不应抛出异常，但捕获到 {exceptions.Count} 个异常");
    }

    #endregion

    private class MockFormatHandler : IFormatHandler
    {
        public FormatType SupportedFormat { get; }
        private readonly string _extension;

        public MockFormatHandler(FormatType supportedFormat, string extension)
        {
            SupportedFormat = supportedFormat;
            _extension = extension;
        }

        public bool CanHandle(string path)
        {
            return path.EndsWith(_extension, StringComparison.OrdinalIgnoreCase);
        }

        public Task<FormatMetadata> ReadMetadataAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FormatMetadata { Type = SupportedFormat, Path = path });
        }

        public Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        public Task WriteAsync(string path, byte[] data, FormatMetadata? metadata = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
