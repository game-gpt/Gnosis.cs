namespace Gnosis.Compiler.Interfaces;

public interface ILexer
{
    IReadOnlyList<Token> Tokenize(string source);
}
