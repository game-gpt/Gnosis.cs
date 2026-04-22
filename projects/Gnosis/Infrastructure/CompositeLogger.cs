namespace Gnosis.Infrastructure;

/// <summary>
/// 组合日志器，支持同时向多个日志目标输出，并支持按目标过滤日志级别
/// </summary>
public class CompositeLogger : ILogger
{
    #region 字段

    private readonly List<ILogger> _loggers = new();
    private readonly Dictionary<ILogger, LogLevel> _loggerLevels = new();

    #endregion

    #region 属性

    /// <summary>
    /// 日志器是否启用
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加日志目标
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="minLevel">该目标的最低日志级别，默认为 Debug</param>
    public void AddLogger(ILogger logger, LogLevel minLevel = LogLevel.Debug)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _loggers.Add(logger);
        _loggerLevels[logger] = minLevel;
    }

    /// <summary>
    /// 移除日志目标
    /// </summary>
    /// <param name="logger">要移除的日志器</param>
    public void RemoveLogger(ILogger logger)
    {
        _loggers.Remove(logger);
        _loggerLevels.Remove(logger);
    }

    /// <summary>
    /// 设置指定日志目标的最低日志级别
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="level">最低日志级别</param>
    public void SetLoggerLogLevel(ILogger logger, LogLevel level)
    {
        if (_loggerLevels.ContainsKey(logger))
        {
            _loggerLevels[logger] = level;
        }
    }

    #endregion

    #region ILogger 实现

    /// <summary>
    /// 输出调试级别日志
    /// </summary>
    public void LogDebug(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Debug))
        {
            logger.LogDebug(message);
        }
    }

    /// <summary>
    /// 输出信息级别日志
    /// </summary>
    public void LogInfo(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Info))
        {
            logger.LogInfo(message);
        }
    }

    /// <summary>
    /// 输出警告级别日志
    /// </summary>
    public void LogWarning(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Warning))
        {
            logger.LogWarning(message);
        }
    }

    /// <summary>
    /// 输出错误级别日志
    /// </summary>
    public void LogError(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Error))
        {
            logger.LogError(message);
        }
    }

    /// <summary>
    /// 输出异常日志
    /// </summary>
    public void LogException(Exception exception, string? message = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Error))
        {
            logger.LogException(exception, message);
        }
    }

    /// <summary>
    /// 输出带属性的调试级别日志
    /// </summary>
    public void LogDebug(string message, params (string Key, object Value)[] properties)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Debug))
        {
            logger.LogDebug(message, properties);
        }
    }

    /// <summary>
    /// 输出带属性的信息级别日志
    /// </summary>
    public void LogInfo(string message, params (string Key, object Value)[] properties)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Info))
        {
            logger.LogInfo(message, properties);
        }
    }

    /// <summary>
    /// 输出带属性的警告级别日志
    /// </summary>
    public void LogWarning(string message, params (string Key, object Value)[] properties)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Warning))
        {
            logger.LogWarning(message, properties);
        }
    }

    /// <summary>
    /// 输出带属性的错误级别日志
    /// </summary>
    public void LogError(string message, params (string Key, object Value)[] properties)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var logger in GetLoggersForLevel(LogLevel.Error))
        {
            logger.LogError(message, properties);
        }
    }

    /// <summary>
    /// 设置所有日志目标的日志级别
    /// </summary>
    public void SetLogLevel(LogLevel level)
    {
        foreach (var logger in _loggers)
        {
            logger.SetLogLevel(level);
        }
    }

    #endregion

    #region 私有方法

    private IEnumerable<ILogger> GetLoggersForLevel(LogLevel level)
    {
        foreach (var logger in _loggers)
        {
            if (_loggerLevels.TryGetValue(logger, out var minLevel) && level >= minLevel)
            {
                yield return logger;
            }
        }
    }

    #endregion
}
