namespace Gnosis.Compiler.ValueObjects;

public sealed record CompilationResult(byte[] Bytecode, string VmSourceCode);
