namespace Gnosis.Assets.Formats;

public interface IFormatHandler
{
    FormatType SupportedFormat { get; }
    
    bool CanHandle(string path);
    Task<FormatMetadata> ReadMetadataAsync(string path, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAsync(string path, byte[] data, FormatMetadata? metadata = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default);
}
