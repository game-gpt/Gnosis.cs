namespace Gnosis.Compiler;

public interface ICompiler
{
    CompilationResult Compile(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false
    );
}
