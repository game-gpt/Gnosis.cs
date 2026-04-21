namespace Gnosis.Compiler;

public interface IParser
{
    AstNode Parse(IReadOnlyList<Token> tokens);
}
