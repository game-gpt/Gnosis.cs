using Oak.Diagnostics;
using Oak.Valkyrie.AST;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public interface IBytecodeGenerator
{
    CompilationResult GenerateFull(AstNode ast, ArchTarget arch, bool isEditorBuild);
}
