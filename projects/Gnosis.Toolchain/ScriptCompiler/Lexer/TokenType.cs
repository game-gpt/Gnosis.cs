namespace Gnosis.Toolchain.ScriptCompiler.Lexer;

public enum TokenType
{
    Unknown,
    Keyword,
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
