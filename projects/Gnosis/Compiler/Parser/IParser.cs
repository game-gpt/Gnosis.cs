using Gnosis.Compiler.Lexer;

namespace Gnosis.Compiler.Parser;

public interface IParser
{
    AstNode Parse(IReadOnlyList<Token> tokens);
}
