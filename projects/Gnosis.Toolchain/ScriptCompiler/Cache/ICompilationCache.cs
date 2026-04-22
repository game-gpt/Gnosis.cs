namespace Gnosis.Toolchain.ScriptCompiler.Cache;

public interface ICompilationCache
{
    bool TryGet(string key, out byte[]? data);

    void Set(string key, byte[] data);

    void Invalidate(string key);

    void InvalidateAll();

    string ComputeKey(string filePath, string content, string macrosHash);
}
