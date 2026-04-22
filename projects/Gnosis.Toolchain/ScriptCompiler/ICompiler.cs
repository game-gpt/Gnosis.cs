namespace Gnosis.Toolchain.ScriptCompiler;

public interface ICompiler
{
    CompilationResult Compile(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false
    );
}
