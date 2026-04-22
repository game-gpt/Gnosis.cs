namespace Gnosis.Asset.Meta;

public sealed record AssetThumbnail(
    int Width,
    int Height,
    byte[] Data
);
