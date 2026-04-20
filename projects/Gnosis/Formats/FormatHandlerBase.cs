using System.Security.Cryptography;
using Gnosis.Formats.Enums;
using Gnosis.Formats.ValueObjects;

namespace Gnosis.Formats;

public abstract class FormatHandlerBase : IFormatHandler
{
    public abstract FormatType SupportedFormat { get; }
    
    public virtual bool CanHandle(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return GetSupportedExtensions().Contains(extension);
    }
    
    public virtual async Task<FormatMetadata> ReadMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Format file not found: {path}");
        }
        
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
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Format file not found: {path}");
        }
        
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }
    
    public virtual async Task WriteAsync(string path, byte[] data, FormatMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllBytesAsync(path, data, cancellationToken);
    }
    
    public virtual Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(path));
    }
    
    protected abstract IReadOnlyList<string> GetSupportedExtensions();
    
    protected virtual async Task<string> ComputeChecksumAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
