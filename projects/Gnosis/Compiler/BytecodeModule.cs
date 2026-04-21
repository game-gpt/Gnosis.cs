namespace Gnosis.Compiler;

public sealed record BytecodeModule(
    string ModuleName,
    byte[] Instructions,
    IReadOnlyList<string> Dependencies);
