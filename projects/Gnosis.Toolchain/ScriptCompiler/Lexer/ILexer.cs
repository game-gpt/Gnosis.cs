namespace Gnosis.Toolchain.ScriptCompiler.Lexer;

public interface ILexer
{
    IReadOnlyList<Token> Tokenize(string source);
}
