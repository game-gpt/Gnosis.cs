using Gnosis.Asset.Format;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Gnosis.Asset.Meta;

public sealed class TextureThumbnailGenerator : IThumbnailGenerator
{
    public bool CanGenerate(string assetPath, FormatType formatType)
    {
        return formatType == FormatType.Texture;
    }

    public async Task<AssetThumbnail?> GenerateAsync(
        string assetPath,
        int maxSize = 128,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(assetPath, cancellationToken).ConfigureAwait(false);

            var (targetWidth, targetHeight) = CalculateSize(image.Width, image.Height, maxSize);

            image.Mutate(x => x.Resize(targetWidth, targetHeight, KnownResamplers.Lanczos3));

            var rgbaData = ExtractRgbaData(image);

            return new AssetThumbnail(targetWidth, targetHeight, rgbaData);
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static (int Width, int Height) CalculateSize(int originalWidth, int originalHeight, int maxSize)
    {
        if (originalWidth <= maxSize && originalHeight <= maxSize)
        {
            return (originalWidth, originalHeight);
        }

        var ratio = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);
        var targetWidth = Math.Max(1, (int)Math.Round(originalWidth * ratio));
        var targetHeight = Math.Max(1, (int)Math.Round(originalHeight * ratio));
        return (targetWidth, targetHeight);
    }

    private static byte[] ExtractRgbaData(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        var data = new byte[width * height * 4];

        image.CopyPixelDataTo(data.AsSpan());

        return data;
    }
}
