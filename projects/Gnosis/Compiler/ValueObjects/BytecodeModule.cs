namespace Gnosis.Compiler.ValueObjects;

public sealed record BytecodeModule(
    string ModuleName,
    byte[] Instructions,
    IReadOnlyList<string> Dependencies);
