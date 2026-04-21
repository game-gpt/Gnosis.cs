namespace Gnosis.Compiler;

public interface IBytecodeGenerator
{
    BytecodeModule Generate(AstNode ast, ArchTarget arch, bool isEditorBuild);
}
