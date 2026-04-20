namespace Gnosis.Compiler;

public interface IMacroTable
{
    void Add(string name, string value);
    string? Get(string name);
    bool Contains(string name);
}
