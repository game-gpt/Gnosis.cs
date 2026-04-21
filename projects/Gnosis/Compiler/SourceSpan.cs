using Gnosis.Compiler.Lexer;

namespace Gnosis.Compiler;

public sealed record SourceSpan(string FilePath, int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public static SourceSpan FromTokens(Token start, Token end)
    {
        return new SourceSpan(string.Empty, start.Line, start.Column, end.Line, end.Column + end.Value.Length);
    }

    public static SourceSpan FromToken(Token token)
    {
        return new SourceSpan(string.Empty, token.Line, token.Column, token.Line, token.Column + token.Value.Length);
    }

    public override string ToString() => $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
}
