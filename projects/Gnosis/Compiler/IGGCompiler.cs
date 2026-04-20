using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler;

public interface IGGCompiler
{
    CompilationResult Compile(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false);
}
