using Gnosis.Toolchain.ScriptCompiler.Lexer;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

public interface IParser
{
    AstNode Parse(IReadOnlyList<Token> tokens);
}
