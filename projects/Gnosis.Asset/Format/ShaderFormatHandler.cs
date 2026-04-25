using System.Text;

namespace Gnosis.Asset.Format;

public class ShaderFormatHandler : FormatHandlerBase
{
    public override FormatType SupportedFormat => FormatType.Shader;

    public async Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        var source = Encoding.UTF8.GetString(data);

        var shaderData = new ShaderData
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Source = source,
            SourcePath = path
        };

        ParseShaderInfo(source, shaderData);

        return shaderData;
    }

    public async Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default)
    {
        var data = Encoding.UTF8.GetBytes(shader.Source);
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

        return ValidateShaderSyntax(source);
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-shader", ".shader", ".ggshader" };
    }

    private static void ParseShaderInfo(string source, ShaderData shaderData)
    {
        var inputs = new List<string>();
        var outputs = new List<string>();
        var uniforms = new List<string>();
        var dependencies = new List<string>();

        var lines = source.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("struct ") && trimmed.Contains("VertexInput"))
            {
                var nameStart = 7;
                var nameEnd = trimmed.IndexOf('{');
                if (nameEnd > nameStart)
                {
                    shaderData.VertexInputStruct = trimmed[nameStart..nameEnd].Trim();
                }
            }
            else if (trimmed.StartsWith("struct ") && trimmed.Contains("VertexOutput"))
            {
                var nameStart = 7;
                var nameEnd = trimmed.IndexOf('{');
                if (nameEnd > nameStart)
                {
                    shaderData.VertexOutputStruct = trimmed[nameStart..nameEnd].Trim();
                }
            }
            else if (trimmed.StartsWith("struct ") && trimmed.Contains("FragmentOutput"))
            {
                var nameStart = 7;
                var nameEnd = trimmed.IndexOf('{');
                if (nameEnd > nameStart)
                {
                    shaderData.FragmentOutputStruct = trimmed[nameStart..nameEnd].Trim();
                }
            }
            else if (trimmed.StartsWith("struct ") && trimmed.Contains("Uniforms"))
            {
                var nameStart = 7;
                var nameEnd = trimmed.IndexOf('{');
                if (nameEnd > nameStart)
                {
                    shaderData.UniformStruct = trimmed[nameStart..nameEnd].Trim();
                }
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

        shaderData.Inputs = inputs;
        shaderData.Outputs = outputs;
        shaderData.Uniforms = uniforms;
        shaderData.Dependencies = dependencies;
    }

    private static bool ValidateShaderSyntax(string source)
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

public record ShaderData
{
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string VertexInputStruct { get; set; } = string.Empty;
    public string VertexOutputStruct { get; set; } = string.Empty;
    public string FragmentOutputStruct { get; set; } = string.Empty;
    public string UniformStruct { get; set; } = string.Empty;
    public IReadOnlyList<string> Inputs { get; set; } = new List<string>();
    public IReadOnlyList<string> Outputs { get; set; } = new List<string>();
    public IReadOnlyList<string> Uniforms { get; set; } = new List<string>();
    public IReadOnlyList<string> Dependencies { get; set; } = new List<string>();
}
