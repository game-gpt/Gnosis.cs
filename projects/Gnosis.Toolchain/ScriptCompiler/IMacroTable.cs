namespace Gnosis.Toolchain.ScriptCompiler;

public interface IMacroTable
{
    void Add(string name, string value);
    string? Get(string name);
    bool Contains(string name);
    IReadOnlyDictionary<string, string> GetAll();
}
