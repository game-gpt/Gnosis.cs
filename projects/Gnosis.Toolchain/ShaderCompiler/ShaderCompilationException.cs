using Oak.Core.Diagnostics;

namespace Gnosis.Toolchain.ShaderCompiler;

public class ShaderCompilationException : Exception
{
    #region Properties

    public IReadOnlyList<DiagnosticMessage> Errors { get; }

    #endregion

    #region Constructors

    public ShaderCompilationException(IReadOnlyList<DiagnosticMessage> errors)
        : base($"着色器编译失败，共 {errors.Count} 个错误")
    {
        Errors = errors;
    }

    #endregion
}