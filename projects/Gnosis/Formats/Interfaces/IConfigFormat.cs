namespace Gnosis.Formats.Interfaces;

public interface IConfigFormat : IFormatHandler
{
    Task<ConfigData> LoadConfigAsync(string path, CancellationToken cancellationToken = default);
    Task SaveConfigAsync(string path, ConfigData config, CancellationToken cancellationToken = default);
    Task<ConfigData> MergeAsync(ConfigData baseConfig, ConfigData overrideConfig, CancellationToken cancellationToken = default);
}

public record ConfigData
{
    public string Name { get; init; } = string.Empty;
    public ConfigFormat Format { get; init; }
    public IReadOnlyDictionary<string, ConfigValue> Values { get; init; } = new Dictionary<string, ConfigValue>();
    public IReadOnlyList<ConfigSection> Sections { get; init; } = new List<ConfigSection>();
}

public record ConfigValue
{
    public string Key { get; init; } = string.Empty;
    public ConfigValueType Type { get; init; }
    public object Value { get; init; } = new object();
    public string? Comment { get; init; }
}

public record ConfigSection
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, ConfigValue> Values { get; init; } = new Dictionary<string, ConfigValue>();
}

public enum ConfigFormat
{
    Unknown = 0,
    Json = 1,
    Xml = 2,
    Yaml = 3,
    Toml = 4,
    Ini = 5,
    Binary = 6
}

public enum ConfigValueType
{
    String = 0,
    Int = 1,
    Float = 2,
    Bool = 3,
    Array = 4,
    Object = 5,
    Null = 6
}
