using Gnosis.Asset.Format;

namespace Gnosis.Asset.Meta;

public interface IThumbnailGenerator
{
    bool CanGenerate(string assetPath, FormatType formatType);

    Task<AssetThumbnail?> GenerateAsync(string assetPath, int maxSize = 128, CancellationToken cancellationToken = default);
}
