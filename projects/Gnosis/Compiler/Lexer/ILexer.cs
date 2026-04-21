namespace Gnosis.Compiler.Lexer;

public interface ILexer
{
    IReadOnlyList<Token> Tokenize(string source);
}
