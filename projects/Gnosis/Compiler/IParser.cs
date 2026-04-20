using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler;

public interface IParser
{
    AstNode Parse(IReadOnlyList<Token> tokens);
}
