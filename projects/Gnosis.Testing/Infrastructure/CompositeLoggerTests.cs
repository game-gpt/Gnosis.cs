using Gnosis.Infrastructure;
using NUnit.Framework;

namespace Gnosis.Testing.Infrastructure;

[TestFixture]
public class CompositeLoggerTests
{
    private CompositeLogger _compositeLogger = null!;
    private ConsoleLogger _consoleLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _compositeLogger = new CompositeLogger();
        _consoleLogger = new ConsoleLogger();
    }

    #region 多目标输出

    [Test]
    public void AddLogger_MessageGoesToAllLoggers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var logFilePath = Path.Combine(tempDir, "composite.log");

        try
        {
            using (var fileLogger = new FileLogger(logFilePath))
            {
                _compositeLogger.AddLogger(_consoleLogger);
                _compositeLogger.AddLogger(fileLogger);

                _compositeLogger.LogInfo("测试消息");
            }

            var content = File.ReadAllText(logFilePath);
            Assert.That(content, Does.Contain("测试消息"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public void AddLogger_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _compositeLogger.AddLogger(null!));
    }

    [Test]
    public void RemoveLogger_StopsSendingMessages()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var logFilePath = Path.Combine(tempDir, "composite_remove.log");

        try
        {
            var fileLogger = new FileLogger(logFilePath);
            _compositeLogger.AddLogger(fileLogger);

            _compositeLogger.LogInfo("移除前消息");

            _compositeLogger.RemoveLogger(fileLogger);
            fileLogger.Dispose();

            using (var fileLogger2 = new FileLogger(logFilePath))
            {
                _compositeLogger.AddLogger(fileLogger2);
                _compositeLogger.LogInfo("移除后消息");
            }

            var content = File.ReadAllText(logFilePath);
            Assert.That(content, Does.Contain("移除前消息"));
            Assert.That(content, Does.Contain("移除后消息"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region 按目标过滤级别

    [Test]
    public void SetLoggerLogLevel_FiltersMessagesByLevel()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var logFilePath = Path.Combine(tempDir, "composite_filter.log");

        try
        {
            using (var fileLogger = new FileLogger(logFilePath))
            {
                fileLogger.SetLogLevel(LogLevel.Debug);

                _compositeLogger.AddLogger(_consoleLogger, LogLevel.Debug);
                _compositeLogger.AddLogger(fileLogger, LogLevel.Error);

                _compositeLogger.LogDebug("调试消息");
                _compositeLogger.LogError("错误消息");
            }

            var content = File.ReadAllText(logFilePath);
            Assert.That(content, Does.Not.Contain("调试消息"));
            Assert.That(content, Does.Contain("错误消息"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region 结构化日志

    [Test]
    public void LogInfo_WithProperties_DispatchesToAllLoggers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var logFilePath = Path.Combine(tempDir, "composite_props.log");

        try
        {
            using (var fileLogger = new FileLogger(logFilePath))
            {
                fileLogger.SetLogLevel(LogLevel.Debug);

                _compositeLogger.AddLogger(fileLogger, LogLevel.Debug);
                _compositeLogger.LogInfo("消息", ("Key", 42));
            }

            var content = File.ReadAllText(logFilePath);
            Assert.That(content, Does.Contain("消息 [Key=42]"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region 异常日志

    [Test]
    public void LogException_DispatchesToAllLoggers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var logFilePath = Path.Combine(tempDir, "composite_exception.log");

        try
        {
            using (var fileLogger = new FileLogger(logFilePath))
            {
                _compositeLogger.AddLogger(fileLogger);

                var exception = new InvalidOperationException("测试异常");
                _compositeLogger.LogException(exception, "上下文");
            }

            var content = File.ReadAllText(logFilePath);
            Assert.That(content, Does.Contain("InvalidOperationException"));
            Assert.That(content, Does.Contain("测试异常"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region 属性

    [Test]
    public void IsEnabled_DefaultIsTrue()
    {
        Assert.That(_compositeLogger.IsEnabled, Is.True);
    }

    [Test]
    public void SetLogLevel_SetsLevelForAllLoggers()
    {
        _compositeLogger.AddLogger(_consoleLogger);
        _compositeLogger.SetLogLevel(LogLevel.Error);

        Assert.DoesNotThrow(() => _compositeLogger.SetLogLevel(LogLevel.Debug));
    }

    #endregion
}
