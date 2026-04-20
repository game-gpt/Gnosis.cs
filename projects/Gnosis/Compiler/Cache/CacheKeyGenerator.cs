using System.Security.Cryptography;
using System.Text;

namespace Gnosis.Compiler.Cache;

public static class CacheKeyGenerator
{
    public static string Generate(string filePath, string content, string macrosHash)
    {
        var normalizedPath = NormalizePath(filePath);
        var contentHash = ComputeHash(content);
        return $"{normalizedPath}:{contentHash}:{macrosHash}";
    }

    public static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string NormalizePath(string filePath)
    {
        return filePath.Replace('\\', '/').ToLowerInvariant();
    }
}
