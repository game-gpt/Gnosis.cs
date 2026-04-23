using System.Globalization;
using Gnosis.Core.Entity;
using Gnosis.Core.Event;
using Gnosis.Core.IO;
using Gnosis.Core.Lifecycle;

namespace Gnosis.Core.Config;

public class ConfigSystem : ISystem
{
    #region 字段

    private readonly IFileSystem _fileSystem;
    private readonly Dictionary<string, string> _values = new();
    private readonly Dictionary<string, string> _sources = new();
    private readonly EntityId _aggregateId;

    #endregion

    #region 属性

    public Action<ConfigChangedEvent>? OnConfigChanged { get; set; }

    #endregion

    #region 构造函数

    public ConfigSystem(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _aggregateId = EntityId.New();
    }

    #endregion

    #region ISystem 实现

    public void Initialize()
    {
    }

    public void Shutdown()
    {
        _values.Clear();
        _sources.Clear();
    }

    #endregion

    #region 加载配置

    public void Load(string path)
    {
        if (!_fileSystem.FileExists(path))
        {
            throw new FileNotFoundException($"配置文件未找到：{path}");
        }

        using var stream = _fileSystem.OpenRead(path);
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

    #region 读取配置

    public T Get<T>(string key)
    {
        if (!_values.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"配置键不存在：{key}");
        }

        return ConvertValue<T>(key, value);
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (!_values.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return ConvertValue<T>(key, value);
    }

    public bool ContainsKey(string key)
    {
        return _values.ContainsKey(key);
    }

    #endregion

    #region 运行时修改

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

    #region 配置层级

    public string? GetSource(string key)
    {
        return _sources.GetValueOrDefault(key);
    }

    #endregion

    #region 私有方法

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

            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new InvalidCastException($"无法将配置键 '{key}' 的值 '{value}' 转换为类型 {targetType.Name}");
        }
    }

    #endregion
}
