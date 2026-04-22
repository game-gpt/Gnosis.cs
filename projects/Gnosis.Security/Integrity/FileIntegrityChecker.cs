using Gnosis.Core.Hash;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Integrity;

public sealed class FileIntegrityChecker
{
    #region 字段

    private readonly Dictionary<string, byte[]> _fileHashes = new();

    #endregion

    #region 属性

    public int RegisteredFileCount => _fileHashes.Count;

    #endregion

    #region 公开方法

    public void Register(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new SecurityException($"文件不存在：{filePath}");
        }

        var bytes = File.ReadAllBytes(filePath);
        var hash = Sha256.Compute(bytes);
        _fileHashes[filePath] = hash;
    }

    public bool Check(string filePath)
    {
        if (!_fileHashes.TryGetValue(filePath, out var originalHash))
        {
            throw new SecurityException($"文件未注册：{filePath}");
        }

        if (!File.Exists(filePath)) return false;

        var bytes = File.ReadAllBytes(filePath);
        var currentHash = Sha256.Compute(bytes);

        return currentHash.AsSpan().SequenceEqual(originalHash);
    }

    public Dictionary<string, bool> CheckAll()
    {
        var results = new Dictionary<string, bool>();

        foreach (var filePath in _fileHashes.Keys)
        {
            results[filePath] = Check(filePath);
        }

        return results;
    }

    public void Unregister(string filePath) => _fileHashes.Remove(filePath);

    #endregion
}
