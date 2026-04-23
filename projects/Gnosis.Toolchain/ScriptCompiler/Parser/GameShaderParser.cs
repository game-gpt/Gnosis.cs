using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Lexer;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     GameShader 语法分析器（基于 Oak.GGShader 的适配层）
/// </summary>
public sealed class GameShaderParser
{
    /// <summary>
    ///     解析 Token 序列，生成 AST
    /// </summary>
    public AstNode Parse(IReadOnlyList<Token> tokens)
    {
        var oakTokens = tokens.Select(ConvertToken).ToList();
        var oakParser = new Oak.GGShader.Parser.GGShaderParser();
        var oakResult = oakParser.Parse(oakTokens);
        return OakShaderAstConverter.Convert(oakResult);
    }

    private static Oak.GGShader.Token ConvertToken(Token token)
    {
        return new Oak.GGShader.Token(
            (Oak.GGShader.TokenType)(int)token.TokenType,
            token.Value,
            token.Line,
            token.Column);
    }
}
