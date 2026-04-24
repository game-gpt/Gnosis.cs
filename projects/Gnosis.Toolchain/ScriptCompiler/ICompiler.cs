using Oak.Diagnostics;
using Oak.Valkyrie.AST;

namespace Gnosis.Toolchain.ScriptCompiler;

public interface ICompiler
{
    CompilationResult Compile(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false);

    CompilationResult CompileSource(
        string source,
        string filePath,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false);
}
