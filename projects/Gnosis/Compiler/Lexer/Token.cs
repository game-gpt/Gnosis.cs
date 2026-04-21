namespace Gnosis.Compiler.Lexer;

public sealed record Token(TokenType TokenType, string Value, int Line, int Column);
