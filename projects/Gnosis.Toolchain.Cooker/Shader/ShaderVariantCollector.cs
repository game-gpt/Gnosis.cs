namespace Gnosis.Toolchain.Cooker.Shader;

public sealed record ShaderVariantKey
{
    public string ShaderName { get; init; } = string.Empty;
    public string PassName { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Defines { get; init; } = new Dictionary<string, string>();
    public ShaderStage Stage { get; init; } = ShaderStage.Fragment;
}

public enum ShaderStage
{
    Vertex,
    Fragment,
    Compute,
    Geometry,
    TessellationControl,
    TessellationEvaluation,
    RayTracing
}

public sealed record ShaderVariant
{
    public ShaderVariantKey Key { get; init; } = new();
    public byte[] SpirvBytes { get; init; } = [];
    public IReadOnlyList<string> UniformNames { get; init; } = [];
    public IReadOnlyList<string> SamplerNames { get; init; } = [];
    public int PushConstantSize { get; init; }
}

public sealed record ShaderVariantCollection
{
    public string CollectionName { get; init; } = string.Empty;
    public IReadOnlyList<ShaderVariant> Variants { get; init; } = [];
    public DateTime CompileTime { get; init; } = DateTime.UtcNow;
    public int TotalVariantCount => Variants.Count;
}

public sealed class ShaderVariantCollector
{
    private readonly List<ShaderVariantKey> _requestedVariants = new();
    private readonly Dictionary<string, List<ShaderVariantKey>> _shaderVariants = new(StringComparer.OrdinalIgnoreCase);

    public void RequestVariant(ShaderVariantKey key)
    {
        if (string.IsNullOrWhiteSpace(key.ShaderName))
        {
            throw new ArgumentException("着色器名称不能为空", nameof(key));
        }

        _requestedVariants.Add(key);

        if (!_shaderVariants.ContainsKey(key.ShaderName))
        {
            _shaderVariants[key.ShaderName] = new List<ShaderVariantKey>();
        }

        _shaderVariants[key.ShaderName].Add(key);
    }

    public void RequestVariants(string shaderName, IEnumerable<ShaderVariantKey> keys)
    {
        foreach (var key in keys)
        {
            var normalizedKey = key with { ShaderName = shaderName };
            RequestVariant(normalizedKey);
        }
    }

    public void RequestAllStages(string shaderName, IEnumerable<KeyValuePair<string, string>> defines)
    {
        var defineDict = defines.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (ShaderStage stage in Enum.GetValues<ShaderStage>())
        {
            RequestVariant(new ShaderVariantKey
            {
                ShaderName = shaderName,
                Stage = stage,
                Defines = defineDict
            });
        }
    }

    public IReadOnlyList<ShaderVariantKey> GetVariantsForShader(string shaderName)
    {
        return _shaderVariants.TryGetValue(shaderName, out var variants)
            ? variants.ToList()
            : Array.Empty<ShaderVariantKey>();
    }

    public IReadOnlyList<string> GetShaderNames()
    {
        return _shaderVariants.Keys.ToList();
    }

    public int TotalVariantCount => _requestedVariants.Count;

    public async Task<ShaderVariantCollection> CollectAndCompileAsync(
        string shaderDirectory,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(shaderDirectory))
        {
            throw new DirectoryNotFoundException($"着色器目录不存在：{shaderDirectory}");
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var compiledVariants = new List<ShaderVariant>();

        foreach (var key in _requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var shaderFile = FindShaderFile(shaderDirectory, key.ShaderName);
            if (shaderFile is null)
            {
                continue;
            }

            var variant = await CompileVariantAsync(shaderFile, key, cancellationToken);
            if (variant is not null)
            {
                compiledVariants.Add(variant);
            }
        }

        var collection = new ShaderVariantCollection
        {
            CollectionName = Path.GetFileName(shaderDirectory),
            Variants = compiledVariants
        };

        await SaveCollectionAsync(collection, outputDirectory, cancellationToken);

        return collection;
    }

    public void Clear()
    {
        _requestedVariants.Clear();
        _shaderVariants.Clear();
    }

    private static string? FindShaderFile(string directory, string shaderName)
    {
        var extensions = new[] { ".gg-shader", ".shader", ".ggs" };

        foreach (var ext in extensions)
        {
            var path = Path.Combine(directory, $"{shaderName}{ext}");
            if (File.Exists(path))
            {
                return path;
            }
        }

        var files = Directory.GetFiles(directory, $"{shaderName}.*");
        return files.FirstOrDefault();
    }

    private static async Task<ShaderVariant?> CompileVariantAsync(string shaderFile, ShaderVariantKey key, CancellationToken cancellationToken)
    {
        try
        {
            var source = await File.ReadAllTextAsync(shaderFile, cancellationToken);

            var defines = key.Defines.Select(kvp => $"#define {kvp.Key} {kvp.Value}");
            var defineBlock = string.Join("\n", defines);

            var variant = new ShaderVariant
            {
                Key = key,
                SpirvBytes = [],
                UniformNames = [],
                SamplerNames = []
            };

            return variant;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static async Task SaveCollectionAsync(ShaderVariantCollection collection, string outputDirectory, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(outputDirectory, $"{collection.CollectionName}.gnosis-shader-variants");

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(0x47535356);
        writer.Write((ushort)1);
        writer.Write(collection.CollectionName);
        writer.Write(collection.Variants.Count);

        foreach (var variant in collection.Variants)
        {
            writer.Write(variant.Key.ShaderName);
            writer.Write(variant.Key.PassName);
            writer.Write((byte)variant.Key.Stage);

            writer.Write(variant.Key.Defines.Count);
            foreach (var (defineKey, defineValue) in variant.Key.Defines)
            {
                writer.Write(defineKey);
                writer.Write(defineValue);
            }

            writer.Write(variant.SpirvBytes.Length);
            writer.Write(variant.SpirvBytes);

            writer.Write(variant.UniformNames.Count);
            foreach (var name in variant.UniformNames)
            {
                writer.Write(name);
            }
        }

        await stream.FlushAsync(cancellationToken);
    }
}
