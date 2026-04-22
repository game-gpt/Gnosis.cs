using Gnosis.Toolchain.ScriptCompiler.Diagnostics;

namespace Gnosis.Toolchain.ShaderCompiler;

public class ShaderCompilationException : Exception
{
    #region Properties

    public IReadOnlyList<Diagnostic> Errors { get; }

    #endregion

    #region Constructors

    public ShaderCompilationException(IReadOnlyList<Diagnostic> errors)
        : base($"着色器编译失败，共 {errors.Count} 个错误")
    {
        Errors = errors;
    }

    #endregion
}