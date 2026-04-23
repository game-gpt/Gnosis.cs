namespace Gnosis.Toolchain.ScriptCompiler.Lexer;

/// <summary>
///     GameScript 词法分析器（基于 Oak.GGScript 的适配层）
/// </summary>
public sealed class GameScriptLexer
{
    /// <summary>
    ///     对源代码进行词法分析，生成 Token 序列
    /// </summary>
    public IReadOnlyList<Token> Tokenize(string source)
    {
        var oakLexer = new Oak.GGScript.Lexer.GGScriptLexer();
        var oakTokens = oakLexer.Tokenize(source);
        return oakTokens.Select(ConvertToken).ToList();
    }

    private static Token ConvertToken(Oak.GGScript.Token oakToken)
    {
        return new Token(
            (TokenType)(int)oakToken.TokenType,
            oakToken.Value,
            oakToken.Line,
            oakToken.Column);
    }
}
