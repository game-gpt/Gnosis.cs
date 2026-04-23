using Gnosis.Core.Diagnostic;
using NUnit.Framework;

namespace Gnosis.Infrastructure;

[TestFixture]
public class FileLoggerTests
{
    private string _tempDir = null!;
    private string _logFilePath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _logFilePath = Path.Combine(_tempDir, "test.log");
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch (IOException)
        {
        }
    }

    #region 写入日志文件

    [Test]
    public void LogInfo_WritesToFile()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogInfo("测试消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("[INFO]"));
        Assert.That(content, Does.Contain("测试消息"));
    }

    [Test]
    public void LogDebug_WritesToFile()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.SetLogLevel(LogLevel.Debug);
            logger.LogDebug("调试消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("[DEBUG]"));
        Assert.That(content, Does.Contain("调试消息"));
    }

    [Test]
    public void LogWarning_WritesToFile()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogWarning("警告消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("[WARN]"));
        Assert.That(content, Does.Contain("警告消息"));
    }

    [Test]
    public void LogError_WritesToFile()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogError("错误消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("[ERROR]"));
        Assert.That(content, Does.Contain("错误消息"));
    }

    [Test]
    public void LogException_WritesToFile()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            var exception = new InvalidOperationException("测试异常");
            logger.LogException(exception, "异常上下文");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("[ERROR]"));
        Assert.That(content, Does.Contain("异常上下文"));
        Assert.That(content, Does.Contain("InvalidOperationException"));
        Assert.That(content, Does.Contain("测试异常"));
    }

    [Test]
    public void LogLine_ContainsTimestamp()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogInfo("消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Match(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]"));
    }

    [Test]
    public void LogLine_FormatIsTimestampLevelMessage()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogInfo("消息");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Match(@"\[\d{4}-.*\] \[INFO\] 消息"));
    }

    #endregion

    #region 结构化日志

    [Test]
    public void LogInfo_WithProperties_FormatsProperties()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.LogInfo("消息", ("UserId", 42), ("Action", "login"));
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Contain("消息 [UserId=42, Action=login]"));
    }

    #endregion

    #region 日志级别过滤

    [Test]
    public void LogDebug_BelowMinLevel_DoesNotWrite()
    {
        using (var logger = new FileLogger(_logFilePath))
        {
            logger.SetLogLevel(LogLevel.Warning);
            logger.LogDebug("不应出现");
        }

        var content = File.ReadAllText(_logFilePath);
        Assert.That(content, Does.Not.Contain("不应出现"));
    }

    #endregion

    #region 日志文件轮转

    [Test]
    public void FileRotation_CreatesBackupWhenSizeExceeded()
    {
        var smallMaxSize = 256L;
        using (var logger = new FileLogger(_logFilePath, maxFileSize: smallMaxSize, maxFileCount: 3))
        {
            for (var i = 0; i < 50; i++)
            {
                logger.LogInfo($"这是一条较长的日志消息，编号 {i:D4}，用于触发文件轮转");
            }
        }

        Assert.That(File.Exists(_logFilePath), Is.True);
        Assert.That(File.Exists($"{_logFilePath}.1"), Is.True);
    }

    [Test]
    public void FileRotation_RespectsMaxFileCount()
    {
        var smallMaxSize = 128L;
        using (var logger = new FileLogger(_logFilePath, maxFileSize: smallMaxSize, maxFileCount: 2))
        {
            for (var i = 0; i < 100; i++)
            {
                logger.LogInfo($"这是一条较长的日志消息，编号 {i:D4}，用于触发文件轮转");
            }
        }

        Assert.That(File.Exists(_logFilePath), Is.True);
        Assert.That(File.Exists($"{_logFilePath}.1"), Is.True);
        Assert.That(File.Exists($"{_logFilePath}.2"), Is.True);
        Assert.That(File.Exists($"{_logFilePath}.3"), Is.False);
    }

    #endregion

    #region 属性

    [Test]
    public void LogFilePath_ReturnsCorrectPath()
    {
        using var logger = new FileLogger(_logFilePath);
        Assert.That(logger.LogFilePath, Is.EqualTo(_logFilePath));
    }

    [Test]
    public void IsEnabled_DefaultIsTrue()
    {
        using var logger = new FileLogger(_logFilePath);
        Assert.That(logger.IsEnabled, Is.True);
    }

    #endregion

    #region 构造函数

    [Test]
    public void Constructor_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FileLogger(null!));
    }

    [Test]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        var deepPath = Path.Combine(_tempDir, "sub1", "sub2", "test.log");
        using (var logger = new FileLogger(deepPath))
        {
            logger.LogInfo("测试");
        }

        Assert.That(File.Exists(deepPath), Is.True);
    }

    #endregion
}
