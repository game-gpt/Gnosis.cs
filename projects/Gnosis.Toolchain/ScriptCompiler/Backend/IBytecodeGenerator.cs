using Oak.Core.Diagnostics;
using Oak.GGScript.AST;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public interface IBytecodeGenerator
{
    CompilationResult GenerateFull(AstNode ast, ArchTarget arch, bool isEditorBuild);
}
