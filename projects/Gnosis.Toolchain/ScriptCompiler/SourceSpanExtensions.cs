using Gnosis.Core.Diagnostic;
using Gnosis.Toolchain.ScriptCompiler.Lexer;

namespace Gnosis.Toolchain.ScriptCompiler;

public static class SourceSpanExtensions
{
    public static SourceSpan ToSourceSpan(this Token token)
    {
        return new SourceSpan(string.Empty, token.Line, token.Column, token.Line, token.Column + token.Value.Length);
    }

    public static SourceSpan ToSourceSpan(this Token start, Token end)
    {
        return new SourceSpan(string.Empty, start.Line, start.Column, end.Line, end.Column + end.Value.Length);
    }
}
