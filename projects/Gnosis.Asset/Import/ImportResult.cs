namespace Gnosis.Asset.Import;

public sealed record ImportResult
{
    public bool Success { get; init; }
    public string AssetPath { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public ImportResult(
        bool success,
        string assetPath,
        string? outputPath = null,
        string? errorMessage = null,
        IReadOnlyList<string>? dependencies = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Success = success;
        AssetPath = assetPath;
        OutputPath = outputPath;
        ErrorMessage = errorMessage;
        Dependencies = dependencies ?? Array.Empty<string>();
        Metadata = metadata;
    }

    public static ImportResult Succeeded(
        string assetPath,
        string outputPath,
        IReadOnlyList<string>? dependencies = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new ImportResult(
            success: true,
            assetPath: assetPath,
            outputPath: outputPath,
            dependencies: dependencies,
            metadata: metadata);
    }

    public static ImportResult Failed(
        string assetPath,
        string errorMessage,
        IReadOnlyList<string>? dependencies = null)
    {
        return new ImportResult(
            success: false,
            assetPath: assetPath,
            errorMessage: errorMessage,
            dependencies: dependencies);
    }
}
