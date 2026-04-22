using System.Security.Cryptography;
using System.Text;

namespace Gnosis.Toolchain.ScriptCompiler.Cache;

public class FileCompilationCache : ICompilationCache
{
    #region Fields

    private readonly string _cacheDirectory;
    private readonly ReaderWriterLockSlim _lock = new();

    #endregion

    #region Constructors

    public FileCompilationCache(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    #endregion

    #region Public Methods

    public bool TryGet(string key, out byte[]? data)
    {
        _lock.EnterReadLock();
        try
        {
            var filePath = GetCacheFilePath(key);

            if (!File.Exists(filePath))
            {
                data = null;
                return false;
            }

            data = File.ReadAllBytes(filePath);
            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Set(string key, byte[] data)
    {
        _lock.EnterWriteLock();
        try
        {
            var filePath = GetCacheFilePath(key);
            File.WriteAllBytes(filePath, data);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Invalidate(string key)
    {
        _lock.EnterWriteLock();
        try
        {
            var filePath = GetCacheFilePath(key);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void InvalidateAll()
    {
        _lock.EnterWriteLock();
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache");

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string ComputeKey(string filePath, string content, string macrosHash)
    {
        return CacheKeyGenerator.Generate(filePath, content, macrosHash);
    }

    #endregion

    #region Private Methods

    private string GetCacheFilePath(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        var fileName = Convert.ToHexString(hash);
        return Path.Combine(_cacheDirectory, $"{fileName}.cache");
    }

    #endregion
}
