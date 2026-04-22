using System.Text.Json;

namespace Gnosis.Asset.Meta;

public static class AssetGuidRegistryPersistence
{
    public static async Task SaveAsync(AssetGuidRegistry registry, string filePath)
    {
        var mappings = registry.GetAllMappings()
            .Select(m => new GuidMappingDto { Guid = m.Guid.ToString(), Path = m.Path })
            .ToList();

        var container = new GuidMappingContainerDto { Mappings = mappings };

        var json = JsonSerializer.Serialize(container, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    public static async Task LoadAsync(string filePath, AssetGuidRegistry registry)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        var container = JsonSerializer.Deserialize<GuidMappingContainerDto>(json);
        if (container?.Mappings is null)
        {
            return;
        }

        foreach (var mapping in container.Mappings)
        {
            if (AssetGuid.TryParse(mapping.Guid, out var guid) && !string.IsNullOrWhiteSpace(mapping.Path))
            {
                registry.Register(mapping.Path, guid);
            }
        }
    }

    private sealed class GuidMappingContainerDto
    {
        public List<GuidMappingDto> Mappings { get; set; } = new();
    }

    private sealed class GuidMappingDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
