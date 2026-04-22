using Gnosis.Asset.Meta;
using Gnosis.Asset.VFS;

namespace Gnosis.Asset.Import;

public sealed class AssetVersionDetector
{
    private readonly AssetHashCache _hashCache;

    public AssetVersionDetector(AssetHashCache hashCache)
    {
        _hashCache = hashCache ?? throw new ArgumentNullException(nameof(hashCache));
    }

    public bool IsUpToDate(string assetPath, IVirtualFileSystem vfs)
    {
        var stream = vfs.OpenRead(assetPath);
        if (stream is null)
        {
            return false;
        }

        AssetHash currentHash;
        try
        {
            currentHash = AssetHashCalculator.ComputeHash(stream);
        }
        finally
        {
            stream.Dispose();
        }

        if (_hashCache.TryGetHash(assetPath, out var cachedHash))
        {
            return currentHash.Equals(cachedHash);
        }

        return false;
    }

    public void UpdateHash(string assetPath, IVirtualFileSystem vfs)
    {
        var stream = vfs.OpenRead(assetPath);
        if (stream is null)
        {
            return;
        }

        try
        {
            var hash = AssetHashCalculator.ComputeHash(stream);
            _hashCache.SetHash(assetPath, hash);
        }
        finally
        {
            stream.Dispose();
        }
    }

    public bool ShouldReimport(string assetPath, IVirtualFileSystem vfs)
    {
        return !IsUpToDate(assetPath, vfs);
    }
}
