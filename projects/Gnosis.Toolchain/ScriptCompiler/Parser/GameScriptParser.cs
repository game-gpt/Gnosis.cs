using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Lexer;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     GameScript 语法分析器（基于 Oak.GGScript 的适配层）
/// </summary>
public sealed class GameScriptParser : IParser
{
    /// <summary>
    ///     解析 Token 序列，生成 AST
    /// </summary>
    public AstNode Parse(IReadOnlyList<Token> tokens)
    {
        var oakTokens = tokens.Select(ConvertToken).ToList();
        var oakParser = new Oak.GGScript.Parser.GGScriptParser();
        var oakResult = oakParser.Parse(oakTokens);
        return OakAstConverter.Convert(oakResult);
    }

    private static Oak.GGScript.Token ConvertToken(Token token)
    {
        return new Oak.GGScript.Token(
            (Oak.GGScript.TokenType)(int)token.TokenType,
            token.Value,
            token.Line,
            token.Column);
    }
}
