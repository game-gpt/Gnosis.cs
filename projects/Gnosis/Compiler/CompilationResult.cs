namespace Gnosis.Compiler;

public sealed record CompilationResult(byte[] Bytecode, string VmSourceCode);
