namespace Gnosis.Graphic.Shader;

public sealed class ShaderCompilationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ShaderCompilationException(string message) : base(message)
    {
        Errors = [message];
    }

    public ShaderCompilationException(string message, IReadOnlyList<string> errors) : base(message)
    {
        Errors = errors;
    }

    public ShaderCompilationException(string message, Exception inner) : base(message, inner)
    {
        Errors = [message];
    }
}
