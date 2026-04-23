using Oak.Core.Diagnostics;
using Oak.GGScript.Lexer;

namespace Gnosis.Toolchain.ScriptCompiler;

public static class SourceSpanExtensions
{
    public static SourceSpan ToSourceSpan(this Token token)
    {
        return new SourceSpan(token.Line, token.Column, token.Line, token.Column + token.Value.Length);
    }

    public static SourceSpan ToSourceSpan(this Token start, Token end)
    {
        return new SourceSpan(start.Line, start.Column, end.Line, end.Column + end.Value.Length);
    }
}
