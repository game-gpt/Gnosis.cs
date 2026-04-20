using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler;

public interface ILexer
{
    IReadOnlyList<Token> Tokenize(string source);
}
