namespace Gnosis.Asset.Meta;

public static class AssetHashCalculator
{
    private const int BufferSize = 8192;

    public static AssetHash ComputeHash(Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return new AssetHash(sha256.Hash!);
    }

    public static AssetHash ComputeHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return new AssetHash(hash);
    }

    public static AssetHash ComputeFileHash(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
        return ComputeHash(stream);
    }

    public static async Task<AssetHash> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return new AssetHash(sha256.Hash!);
    }

    public static async Task<AssetHash> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        return await ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
