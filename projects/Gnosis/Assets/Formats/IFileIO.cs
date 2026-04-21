namespace Gnosis.Assets.Formats;

public interface IFileIO
{
    Stream? OpenRead(string path);

    Stream? OpenWrite(string path);

    bool Exists(string path);

    void CreateDirectory(string path);

    IEnumerable<string> GetFiles(string path, string searchPattern = "*");

    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);
}
