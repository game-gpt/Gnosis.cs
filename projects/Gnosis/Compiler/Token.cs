namespace Gnosis.Compiler;

public sealed record Token(TokenType TokenType, string Value, int Line, int Column);
