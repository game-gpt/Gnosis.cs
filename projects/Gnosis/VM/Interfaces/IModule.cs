namespace Gnosis.VM.Interfaces;

public interface IModule
{
    string Name { get; }
    IReadOnlyList<byte> Instructions { get; }
    IReadOnlyDictionary<string, int> NativeBindings { get; }
    int EntryPoint { get; }
    IReadOnlyDictionary<string, object?> Constants { get; }
    IReadOnlyList<string> ExportedSymbols { get; }
    IReadOnlyList<string> ImportedSymbols { get; }
    int Version { get; }
    bool IsValid { get; }
}
