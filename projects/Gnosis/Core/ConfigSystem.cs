using System.Globalization;
using Gnosis.Core.Events;
using Gnosis.ECS;
using Gnosis.Infrastructure;

namespace Gnosis.Core;

/// <summary>
/// 配置系统，负责加载、读取和运行时修改配置
/// </summary>
public class ConfigSystem : ISystem
{
    #region Fields

    private readonly IVirtualFileSystem _vfs;
    private readonly Dictionary<string, string> _values = new();
    private readonly Dictionary<string, string> _sources = new();
    private readonly EntityId _aggregateId;

    #endregion

    #region Properties

    /// <summary>
    /// 配置变更事件回调
    /// </summary>
    public Action<ConfigChangedEvent>? OnConfigChanged { get; set; }

    #endregion

    #region Constructor

    public ConfigSystem(IVirtualFileSystem vfs)
    {
        _vfs = vfs;
        _aggregateId = EntityId.New();
    }

    #endregion

    #region ISystem Implementation

    public SystemPhase Phase => SystemPhase.Initialization;

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
    }

    public void Shutdown()
    {
        _values.Clear();
        _sources.Clear();
    }

    #endregion

    #region Load

    /// <summary>
    /// 从指定路径加载配置文件，支持 .cfg 格式（key=value，# 注释，空行忽略）
    /// </summary>
    /// <param name="path">配置文件路径</param>
    /// <exception cref="FileNotFoundException">配置文件不存在时抛出</exception>
    /// <exception cref="FormatException">配置行格式无效时抛出</exception>
    public void Load(string path)
    {
        var stream = _vfs.OpenRead(path)
            ?? throw new FileNotFoundException($"配置文件未找到：{path}");

        using var reader = new StreamReader(stream);
        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex < 0)
            {
                throw new FormatException($"配置文件 {path} 第 {lineNumber} 行格式无效，缺少 '=' 分隔符：{trimmed}");
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            _values[key] = value;
            _sources[key] = path;
        }
    }

    #endregion

    #region Get

    /// <summary>
    /// 获取指定键的配置值并转换为目标类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>转换后的配置值</returns>
    /// <exception cref="KeyNotFoundException">键不存在时抛出</exception>
    /// <exception cref="InvalidCastException">类型转换失败时抛出</exception>
    public T Get<T>(string key)
    {
        if (!_values.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"配置键不存在：{key}");
        }

        return ConvertValue<T>(key, value);
    }

    /// <summary>
    /// 获取指定键的配置值并转换为目标类型，键不存在时返回默认值
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">键不存在时的默认值</param>
    /// <returns>转换后的配置值或默认值</returns>
    public T Get<T>(string key, T defaultValue)
    {
        if (!_values.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return ConvertValue<T>(key, value);
    }

    #endregion

    #region ContainsKey

    /// <summary>
    /// 检查指定配置键是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>键存在返回 true，否则返回 false</returns>
    public bool ContainsKey(string key)
    {
        return _values.ContainsKey(key);
    }

    #endregion

    #region Set

    /// <summary>
    /// 设置配置键值，并触发 ConfigChangedEvent
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="value">新的配置值</param>
    public void Set(string key, string value)
    {
        _values.TryGetValue(key, out var oldValue);

        _values[key] = value;

        if (!_sources.ContainsKey(key))
        {
            _sources[key] = "runtime";
        }

        var evt = new ConfigChangedEvent(_aggregateId, key, oldValue, value);
        OnConfigChanged?.Invoke(evt);
    }

    #endregion

    #region GetSource

    /// <summary>
    /// 获取指定配置键的来源文件路径
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>来源文件路径，键不存在时返回 null</returns>
    public string? GetSource(string key)
    {
        return _sources.TryGetValue(key, out var source) ? source : null;
    }

    #endregion

    #region Private Methods

    private static T ConvertValue<T>(string key, string value)
    {
        var targetType = typeof(T);

        try
        {
            if (targetType == typeof(string))
            {
                return (T)(object)value;
            }

            if (targetType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }

            if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(long))
            {
                return (T)(object)long.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(float))
            {
                return (T)(object)float.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
            }

            return (T)System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new InvalidCastException($"无法将配置键 '{key}' 的值 '{value}' 转换为类型 {targetType.Name}");
        }
    }

    #endregion
}
