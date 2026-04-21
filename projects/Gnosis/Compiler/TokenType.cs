namespace Gnosis.Compiler;

public enum TokenType
{
    Unknown,
    Keyword,
    TypeKeyword,
    Identifier,
    Number,
    String,
    Literal,
    Operator,
    Punctuation,
    Delimiter,
    Attribute,
    MetaBlockStart,
    MetaBlockEnd,
    MetaExpression,
    TemplateDirective,
    StyleClass,
    Comment,
    Eof
}
