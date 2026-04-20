namespace Gnosis.Compiler.Interfaces;

public interface IParser
{
    AstNode Parse(IReadOnlyList<Token> tokens);
}
