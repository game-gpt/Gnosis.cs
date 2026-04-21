using System.Linq;

namespace Gnosis.Infrastructure;

/// <summary>
/// 控制台日志器，支持控制台输出和文件日志
/// </summary>
public class ConsoleLogger : ILogger
{
    #region 字段

    private LogLevel _level = LogLevel.Info;
    private StreamWriter? _fileWriter;
    private readonly object _lock = new();

    #endregion

    #region 属性

    /// <summary>
    /// 日志器是否启用
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// 日志文件路径，为 null 时不输出文件日志
    /// </summary>
    public string? LogFilePath { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化控制台日志器
    /// </summary>
    public ConsoleLogger()
    {
    }

    /// <summary>
    /// 初始化控制台日志器并指定日志文件路径
    /// </summary>
    /// <param name="logFilePath">日志文件路径</param>
    public ConsoleLogger(string logFilePath)
    {
        LogFilePath = logFilePath;

        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _fileWriter = new StreamWriter(logFilePath, append: true)
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

    #region 公开方法

    /// <summary>
    /// 关闭文件日志流
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _fileWriter?.Close();
            _fileWriter = null;
        }
    }

    #endregion

    #region 私有方法

    private void WriteLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{level}] {message}";

        Console.WriteLine(line);

        lock (_lock)
        {
            _fileWriter?.WriteLine(line);
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
