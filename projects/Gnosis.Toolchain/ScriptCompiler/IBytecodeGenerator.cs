namespace Gnosis.Toolchain.ScriptCompiler;

public interface IBytecodeGenerator
{
    BytecodeModule Generate(AstNode ast, ArchTarget arch, bool isEditorBuild);
}
