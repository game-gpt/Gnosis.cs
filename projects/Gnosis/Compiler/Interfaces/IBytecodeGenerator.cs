namespace Gnosis.Compiler.Interfaces;

public interface IBytecodeGenerator
{
    BytecodeModule Generate(AstNode ast, ArchTarget arch, bool isEditorBuild);
}
