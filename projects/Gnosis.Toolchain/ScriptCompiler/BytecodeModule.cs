namespace Gnosis.Toolchain.ScriptCompiler;

public sealed record BytecodeModule(
    string ModuleName,
    byte[] Instructions,
    IReadOnlyList<string> Dependencies);
