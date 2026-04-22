using System.Linq;

namespace Gnosis.Core.Diagnostic;

/// <summary>
/// 文件日志器，将日志写入文件，支持日志文件轮转
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    #region 字段

    private readonly string _logFilePath;
    private readonly long _maxFileSize;
    private readonly int _maxFileCount;
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private LogLevel _level = LogLevel.Info;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 日志器是否启用
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// 单个日志文件最大大小（字节），默认 10MB
    /// </summary>
    public long MaxFileSize => _maxFileSize;

    /// <summary>
    /// 保留的历史日志文件最大数量，默认 5
    /// </summary>
    public int MaxFileCount => _maxFileCount;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化文件日志器
    /// </summary>
    /// <param name="logFilePath">日志文件路径</param>
    /// <param name="maxFileSize">单个日志文件最大大小（字节），默认 10MB</param>
    /// <param name="maxFileCount">保留的历史日志文件最大数量，默认 5</param>
    public FileLogger(string logFilePath, long maxFileSize = 10 * 1024 * 1024, int maxFileCount = 5)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _maxFileSize = maxFileSize;
        _maxFileCount = maxFileCount;

        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(logFilePath, append: true)
        {
            AutoFlush = true
        };
    }

    #endregion

    #region ILogger 实现

    /// <summary>
    /// 输出调试级别日志
    /// </summary>
    public void LogDebug(string message)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
        {
            WriteLog("DEBUG", message);
        }
    }

    /// <summary>
    /// 输出信息级别日志
    /// </summary>
    public void LogInfo(string message)
    {
        if (IsEnabled && _level <= LogLevel.Info)
        {
            WriteLog("INFO", message);
        }
    }

    /// <summary>
    /// 输出警告级别日志
    /// </summary>
    public void LogWarning(string message)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
        {
            WriteLog("WARN", message);
        }
    }

    /// <summary>
    /// 输出错误级别日志
    /// </summary>
    public void LogError(string message)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            WriteLog("ERROR", message);
        }
    }

    /// <summary>
    /// 输出异常日志
    /// </summary>
    public void LogException(Exception exception, string? message = null)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            if (message != null)
            {
                WriteLog("ERROR", message);
            }
            WriteLog("ERROR", $"{exception.GetType().Name}: {exception.Message}");
            WriteLog("ERROR", exception.StackTrace ?? string.Empty);
        }
    }

    /// <summary>
    /// 输出带属性的调试级别日志
    /// </summary>
    public void LogDebug(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
        {
            WriteLog("DEBUG", FormatProperties(message, properties));
        }
    }

    /// <summary>
    /// 输出带属性的信息级别日志
    /// </summary>
    public void LogInfo(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Info)
        {
            WriteLog("INFO", FormatProperties(message, properties));
        }
    }

    /// <summary>
    /// 输出带属性的警告级别日志
    /// </summary>
    public void LogWarning(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
        {
            WriteLog("WARN", FormatProperties(message, properties));
        }
    }

    /// <summary>
    /// 输出带属性的错误级别日志
    /// </summary>
    public void LogError(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            WriteLog("ERROR", FormatProperties(message, properties));
        }
    }

    /// <summary>
    /// 设置日志级别
    /// </summary>
    public void SetLogLevel(LogLevel level)
    {
        _level = level;
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放文件日志器资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _writer?.Close();
            _writer = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region 私有方法

    private void WriteLog(string level, string message)
    {
        lock (_lock)
        {
            if (_writer == null)
            {
                return;
            }

            CheckFileRotation();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] [{level}] {message}";
            _writer.WriteLine(line);
        }
    }

    private void CheckFileRotation()
    {
        if (_writer == null)
        {
            return;
        }

        try
        {
            var fileInfo = new FileInfo(_logFilePath);
            if (!fileInfo.Exists || fileInfo.Length < _maxFileSize)
            {
                return;
            }

            _writer.Close();
            _writer = null;

            RotateFiles();

            _writer = new StreamWriter(_logFilePath, append: true)
            {
                AutoFlush = true
            };
        }
        catch (IOException)
        {
        }
    }

    private void RotateFiles()
    {
        for (var i = _maxFileCount - 1; i >= 1; i--)
        {
            var sourceFile = $"{_logFilePath}.{i}";
            var destFile = $"{_logFilePath}.{i + 1}";

            if (File.Exists(destFile))
            {
                File.Delete(destFile);
            }

            if (File.Exists(sourceFile))
            {
                File.Move(sourceFile, destFile);
            }
        }

        var firstBackup = $"{_logFilePath}.1";
        if (File.Exists(firstBackup))
        {
            File.Delete(firstBackup);
        }

        if (File.Exists(_logFilePath))
        {
            File.Move(_logFilePath, firstBackup);
        }
    }

    private static string FormatProperties(string message, (string Key, object Value)[] properties)
    {
        if (properties == null || properties.Length == 0)
        {
            return message;
        }

        var props = string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
        return $"{message} [{props}]";
    }

    #endregion
}
