using Gnosis.Asset.Format;

namespace Gnosis.Asset.Meta;

public sealed class DefaultThumbnailGenerator : IThumbnailGenerator
{
    private static readonly byte GrayR = 128;
    private static readonly byte GrayG = 128;
    private static readonly byte GrayB = 128;
    private static readonly byte GrayA = 255;

    public bool CanGenerate(string assetPath, FormatType formatType)
    {
        return true;
    }

    public Task<AssetThumbnail?> GenerateAsync(
        string assetPath,
        int maxSize = 128,
        CancellationToken cancellationToken = default)
    {
        var data = new byte[maxSize * maxSize * 4];

        for (var i = 0; i < maxSize * maxSize; i++)
        {
            var offset = i * 4;
            data[offset] = GrayR;
            data[offset + 1] = GrayG;
            data[offset + 2] = GrayB;
            data[offset + 3] = GrayA;
        }

        var thumbnail = new AssetThumbnail(maxSize, maxSize, data);
        return Task.FromResult<AssetThumbnail?>(thumbnail);
    }
}
