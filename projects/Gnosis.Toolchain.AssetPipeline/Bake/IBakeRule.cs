using Gnosis.Asset.Format;
using Gnosis.Asset.Meta;
using Gnosis.Toolchain.AssetPipeline.Graph;

namespace Gnosis.Toolchain.AssetPipeline.Bake;

public sealed record BakeResult
{
    public bool Success { get; init; }
    public string AssetPath { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public FormatType FormatType { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }

    public static BakeResult Succeeded(string assetPath, string outputPath, FormatType formatType, IReadOnlyList<string> dependencies, TimeSpan duration)
    {
        return new BakeResult
        {
            Success = true,
            AssetPath = assetPath,
            OutputPath = outputPath,
            FormatType = formatType,
            Dependencies = dependencies,
            Duration = duration
        };
    }

    public static BakeResult Failed(string assetPath, FormatType formatType, string errorMessage)
    {
        return new BakeResult
        {
            Success = false,
            AssetPath = assetPath,
            FormatType = formatType,
            ErrorMessage = errorMessage
        };
    }

    public static BakeResult UpToDate(string assetPath, string outputPath, FormatType formatType)
    {
        return new BakeResult
        {
            Success = true,
            AssetPath = assetPath,
            OutputPath = outputPath,
            FormatType = formatType
        };
    }
}

public interface IBakeRule
{
    FormatType TargetFormatType { get; }

    bool CanBake(string assetPath, FormatType formatType);

    Task<BakeResult> BakeAsync(string assetPath, string outputDirectory, IFormatRegistry formatRegistry, CancellationToken cancellationToken = default);

    IReadOnlyList<string> CollectDependencies(string assetPath, IFormatRegistry formatRegistry);
}
