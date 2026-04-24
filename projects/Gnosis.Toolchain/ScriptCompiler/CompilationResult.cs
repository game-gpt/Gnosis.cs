namespace Gnosis.Toolchain.ScriptCompiler;

public sealed record CompilationResult(
    byte[] Bytecode,
    string VmSourceCode,
    byte[]? DebugInfo = null);
