namespace Gnosis.Compiler.ValueObjects;

public enum TokenType
{
    Unknown,
    Keyword,
    Identifier,
    Number,
    String,
    Operator,
    Punctuation,
    MetaBlockStart,
    MetaBlockEnd,
    MetaExpression,
    Eof
}
