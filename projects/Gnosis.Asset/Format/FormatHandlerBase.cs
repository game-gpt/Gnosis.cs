using System.Security.Cryptography;
using Gnosis.Asset.Format.Compression;

namespace Gnosis.Asset.Format;

public abstract class FormatHandlerBase : IFormatHandler
{
    protected IFileIO FileIO { get; }

    protected FormatHandlerBase()
    {
        FileIO = new PhysicalFileIO();
    }

    protected FormatHandlerBase(IFileIO fileIO)
    {
        FileIO = fileIO;
    }

    public abstract FormatType SupportedFormat { get; }

    public virtual bool CanHandle(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return GetSupportedExtensions().Contains(extension);
    }

    public virtual async Task<FormatMetadata> ReadMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到格式文件：{path}");
        }

        var fileInfo = new FileInfo(path);
        var checksum = await ComputeChecksumAsync(path, cancellationToken);

        return new FormatMetadata
        {
            Type = SupportedFormat,
            Name = Path.GetFileNameWithoutExtension(path),
            Path = path,
            Size = fileInfo.Length,
            Checksum = checksum,
            Version = 1
        };
    }

    public virtual async Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到格式文件：{path}");
        }

        var data = await FileIO.ReadAllBytesAsync(path, cancellationToken);
        return data;
    }

    public virtual async Task<byte[]> ReadAsync(string path, FormatMetadata? metadata, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);

        if (metadata is not null && metadata.Compression != CompressionType.None)
        {
            data = CompressionService.Decompress(data, metadata.Compression);
        }

        return data;
    }

    public virtual async Task WriteAsync(string path, byte[] data, FormatMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var writeData = metadata is not null && metadata.Compression != CompressionType.None
            ? CompressionService.Compress(data, metadata.Compression)
            : data;

        await FileIO.WriteAllBytesAsync(path, writeData, cancellationToken);
    }

    public virtual Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FileIO.Exists(path));
    }

    protected abstract IReadOnlyList<string> GetSupportedExtensions();

    protected virtual async Task<string> ComputeChecksumAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = FileIO.OpenRead(path);
        if (stream is null)
        {
            throw new FileNotFoundException($"未找到格式文件：{path}");
        }

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
