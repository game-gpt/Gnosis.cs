using Gnosis.Asset.Format;

namespace Gnosis.Asset.Import;

public interface IAssetImporter
{
    bool CanImport(string assetPath, FormatType formatType);

    Task<ImportResult> ImportAsync(ImportContext context, string assetPath, CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetDependencies(string assetPath);
}
