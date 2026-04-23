namespace Gnosis.Toolchain.ScriptCompiler.Lexer;

/// <summary>
///     GameShader 词法分析器（基于 Oak.GGShader 的适配层）
/// </summary>
public sealed class GameShaderLexer : ILexer
{
    /// <summary>
    ///     对源代码进行词法分析，生成 Token 序列
    /// </summary>
    public IReadOnlyList<Token> Tokenize(string source)
    {
        var oakLexer = new Oak.GGShader.Lexer.GGShaderLexer();
        var oakTokens = oakLexer.Tokenize(source);
        return oakTokens.Select(ConvertToken).ToList();
    }

    private static Token ConvertToken(Oak.GGShader.Token oakToken)
    {
        return new Token(
            (TokenType)(int)oakToken.TokenType,
            oakToken.Value,
            oakToken.Line,
            oakToken.Column);
    }
}
