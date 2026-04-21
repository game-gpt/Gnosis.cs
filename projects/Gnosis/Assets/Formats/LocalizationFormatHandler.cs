using System.Text.Json;

namespace Gnosis.Assets.Formats;

public class LocalizationFormatHandler : FormatHandlerBase, ILocalizationFormat
{
    public override FormatType SupportedFormat => FormatType.Localization;
    
    public async Task<LocalizationData> LoadLocalizationAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<LocalizationData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize localization: {path}");
    }
    
    public async Task SaveLocalizationAsync(string path, LocalizationData localization, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(localization, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public Task<LocalizationData> MergeAsync(LocalizationData baseData, LocalizationData overrideData, CancellationToken cancellationToken = default)
    {
        var mergedEntries = new Dictionary<string, string>(baseData.Entries);
        
        foreach (var kvp in overrideData.Entries)
        {
            mergedEntries[kvp.Key] = kvp.Value;
        }
        
        var mergedData = baseData with { Entries = mergedEntries };
        
        return Task.FromResult(mergedData);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".po", ".mo", ".json", ".resx", ".xliff", ".scirptlocale" };
    }
}
