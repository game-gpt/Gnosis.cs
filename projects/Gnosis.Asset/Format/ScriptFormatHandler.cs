using System.Text;

namespace Gnosis.Asset.Format;

public class ScriptFormatHandler : FormatHandlerBase
{
    public override FormatType SupportedFormat => FormatType.Script;

    public async Task<ScriptData> LoadScriptAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        var source = Encoding.UTF8.GetString(data);

        var scriptData = new ScriptData
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Source = source,
            SourcePath = path
        };

        ParseScriptHeader(source, scriptData);

        return scriptData;
    }

    public async Task SaveScriptAsync(string path, ScriptData script, CancellationToken cancellationToken = default)
    {
        var data = Encoding.UTF8.GetBytes(script.Source);
        await WriteAsync(path, data, null, cancellationToken);
    }

    public override async Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            return false;
        }

        var data = await ReadAsync(path, cancellationToken);
        var source = Encoding.UTF8.GetString(data);

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return ValidateScriptSyntax(source);
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-script", ".script", ".ggscript" };
    }

    private static void ParseScriptHeader(string source, ScriptData scriptData)
    {
        var lines = source.Split('\n');
        var systemName = string.Empty;
        var dependencies = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("system "))
            {
                var nameEnd = trimmed.IndexOf('{');
                if (nameEnd > 7)
                {
                    systemName = trimmed[7..nameEnd].Trim();
                }
            }
            else if (trimmed.StartsWith("component "))
            {
                if (string.IsNullOrEmpty(scriptData.ComponentName))
                {
                    var nameEnd = trimmed.IndexOf('{');
                    if (nameEnd > 10)
                    {
                        scriptData.ComponentName = trimmed[10..nameEnd].Trim();
                    }
                }
            }
            else if (trimmed.StartsWith("let ") || trimmed.StartsWith("micro ") || trimmed.StartsWith("struct "))
            {
                continue;
            }
            else if (trimmed.Contains("asset.load<"))
            {
                var start = trimmed.IndexOf('"') + 1;
                var end = trimmed.LastIndexOf('"');
                if (start > 0 && end > start)
                {
                    dependencies.Add(trimmed[start..end]);
                }
            }
        }

        scriptData.SystemName = systemName;
        scriptData.Dependencies = dependencies;
    }

    private static bool ValidateScriptSyntax(string source)
    {
        var braceCount = 0;
        foreach (var c in source)
        {
            if (c == '{') braceCount++;
            if (c == '}') braceCount--;
            if (braceCount < 0) return false;
        }

        return braceCount == 0;
    }
}

public record ScriptData
{
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public IReadOnlyList<string> Dependencies { get; set; } = new List<string>();
}
