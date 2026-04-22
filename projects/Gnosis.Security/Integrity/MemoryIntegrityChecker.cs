using Gnosis.Core.Hash;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Integrity;

public sealed class MemoryIntegrityChecker
{
    #region 字段

    private readonly Dictionary<string, MemoryRegion> _regions = new();

    #endregion

    #region 属性

    public int RegisteredRegionCount => _regions.Count;

    #endregion

    #region 公开方法

    public void Register(string regionId, Func<byte[]> bytesProvider)
    {
        var currentBytes = bytesProvider();
        var hash = Sha256.Compute(currentBytes);
        _regions[regionId] = new MemoryRegion(bytesProvider, hash);
    }

    public bool Check(string regionId)
    {
        if (!_regions.TryGetValue(regionId, out var region))
        {
            throw new SecurityException($"内存区域未注册：{regionId}");
        }

        var currentBytes = region.BytesProvider();
        var currentHash = Sha256.Compute(currentBytes);

        return currentHash.AsSpan().SequenceEqual(region.OriginalHash);
    }

    public Dictionary<string, bool> CheckAll()
    {
        var results = new Dictionary<string, bool>();

        foreach (var regionId in _regions.Keys)
        {
            results[regionId] = Check(regionId);
        }

        return results;
    }

    public void Unregister(string regionId) => _regions.Remove(regionId);

    #endregion

    private sealed record MemoryRegion(Func<byte[]> BytesProvider, byte[] OriginalHash);
}
