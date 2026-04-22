namespace Gnosis.Core.IO;

public interface IFileSystem
{
    Stream OpenRead(string path);

    Stream OpenWrite(string path);

    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    void DeleteFile(string path);

    void DeleteDirectory(string path, bool recursive);

    string[] GetFiles(string path, string searchPattern = "*");

    string[] GetDirectories(string path);

    Task<Stream> OpenReadAsync(string path);

    Task<Stream> OpenWriteAsync(string path);
}
