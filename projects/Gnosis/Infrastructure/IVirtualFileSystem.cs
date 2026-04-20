namespace Gnosis.Infrastructure;

public interface IVirtualFileSystem
{
    void Mount(string path, IFileSystem fileSystem);
    void Unmount(string path);
    
    Stream? OpenRead(string path);
    Stream? OpenWrite(string path);
    
    bool FileExists(string path);
    bool DirectoryExists(string path);
    
    void CreateDirectory(string path);
    void DeleteFile(string path);
    
    IEnumerable<string> GetFiles(string path, string searchPattern = "*");
    IEnumerable<string> GetDirectories(string path);
}

public interface IFileSystem
{
    Stream? OpenRead(string path);
    Stream? OpenWrite(string path);
    
    bool FileExists(string path);
    bool DirectoryExists(string path);
    
    void CreateDirectory(string path);
    void DeleteFile(string path);
    
    IEnumerable<string> GetFiles(string path, string searchPattern = "*");
    IEnumerable<string> GetDirectories(string path);
}
