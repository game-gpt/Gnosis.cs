using System.Text.Json;
using Gnosis.Formats.Enums;

namespace Gnosis.Formats;

public class ConfigFormatHandler : FormatHandlerBase, IConfigFormat
{
    public override FormatType SupportedFormat => FormatType.Config;
    
    public async Task<ConfigData> LoadConfigAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ConfigData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize config: {path}");
    }
    
    public async Task SaveConfigAsync(string path, ConfigData config, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(config, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public Task<ConfigData> MergeAsync(ConfigData baseConfig, ConfigData overrideConfig, CancellationToken cancellationToken = default)
    {
        var mergedValues = new Dictionary<string, ConfigValue>(baseConfig.Values);
        
        foreach (var kvp in overrideConfig.Values)
        {
            mergedValues[kvp.Key] = kvp.Value;
        }
        
        var mergedConfig = baseConfig with { Values = mergedValues };
        
        return Task.FromResult(mergedConfig);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".codeonfig" };
    }
}
