using System.Text;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Compiler.Frontend;

public class GgScriptLexer : ILexer
{
    #region Fields

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "let", "mut", "micro", "component", "system", "query", "widget", "scene",
        "plugin", "import", "export", "return", "if", "else", "loop", "while",
        "create_entity", "destroy_entity", "struct", "true", "false", "null",
        "new", "in", "foreach", "match", "case", "end", "as"
    };

    private static readonly HashSet<string> TypeKeywords = new(StringComparer.Ordinal)
    {
        "i8", "i16", "i32", "i64",
        "u8", "u16", "u32", "u64",
        "f32", "f64",
        "bool", "string", "Entity",
        "vec2", "vec3", "vec4",
        "mat2", "mat3", "mat4",
        "Promise", "Map", "Set", "Array"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%",
        "=", "==", "!=", "<", ">", "<=", ">=",
        "+=", "-=", "*=", "/=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>",
        "=>", "->", "::", "??"
    };

    private static readonly HashSet<char> Delimiters = new()
    {
        '(', ')', '{', '}', ',', ';', '.'
    };

    private string _source = string.Empty;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private DiagnosticSink? _diagnostics;

    #endregion

    #region Constructors

    public GgScriptLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public IReadOnlyList<Token> Tokenize(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
            {
                break;
            }

            var token = ScanToken();

            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token(TokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }

    #endregion

    #region Private Methods

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek()
    {
        if (_position >= _source.Length)
        {
            return '\0';
        }

        return _source[_position];
    }

    private char PeekNext()
    {
        if (_position + 1 >= _source.Length)
        {
            return '\0';
        }

        return _source[_position + 1];
    }

    private char Advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    Advance();
                    break;
                case '#':
                    SkipLineComment();
                    break;
                case '/':
                    if (PeekNext() == '/')
                    {
                        SkipLineComment();
                    }
                    else if (PeekNext() == '*')
                    {
                        SkipBlockComment();
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
        }
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
    }

    private void SkipBlockComment()
    {
        Advance();
        Advance();

        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '/' && PeekNext() == '*')
            {
                Advance();
                Advance();
                depth++;
            }
            else if (Peek() == '*' && PeekNext() == '/')
            {
                Advance();
                Advance();
                depth--;
            }
            else
            {
                Advance();
            }
        }

        if (depth > 0)
        {
            _diagnostics?.AddWarning(
                string.Empty,
                new SourceSpan(string.Empty, _line, _column, _line, _column),
                "GG0001",
                "未闭合的块注释");
        }
    }

    private Token? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == '<' && PeekNext() == '%')
        {
            return ScanMetaBlock(startLine, startColumn);
        }

        var c = Peek();

        if (c == '"' || c == '\'')
        {
            return ScanString(startLine, startColumn);
        }

        if (char.IsDigit(c))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeyword(startLine, startColumn);
        }

        if (c == '[')
        {
            var next = PeekNext();

            if (next == '_' || char.IsLetter(next))
            {
                return ScanAttribute(startLine, startColumn);
            }

            Advance();
            return new Token(TokenType.Delimiter, "[", startLine, startColumn);
        }

        if (c == ']')
        {
            Advance();
            return new Token(TokenType.Delimiter, "]", startLine, startColumn);
        }

        if (IsOperatorStart(c))
        {
            return ScanOperator(startLine, startColumn);
        }

        if (Delimiters.Contains(c))
        {
            Advance();
            return new Token(TokenType.Delimiter, c.ToString(), startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(
            string.Empty,
            new SourceSpan(string.Empty, startLine, startColumn, startLine, startColumn + 1),
            "GG0002",
            $"意外的字符 '{c}'");

        return null;
    }

    private Token ScanMetaBlock(int startLine, int startColumn)
    {
        Advance();
        Advance();

        if (Peek() == '=')
        {
            Advance();

            var content = ScanMetaContent();

            return new Token(TokenType.MetaExpression, content, startLine, startColumn);
        }

        var blockContent = ScanMetaContent();

        return new Token(TokenType.MetaBlockStart, blockContent, startLine, startColumn);
    }

    private string ScanMetaContent()
    {
        var sb = new StringBuilder();
        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '<' && PeekNext() == '%')
            {
                Advance();
                Advance();
                depth++;
                sb.Append("<%");
            }
            else if (Peek() == '%' && PeekNext() == '>')
            {
                Advance();
                Advance();
                depth--;

                if (depth > 0)
                {
                    sb.Append("%>");
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        return sb.ToString().Trim();
    }

    private Token ScanString(int startLine, int startColumn)
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                {
                    break;
                }

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
        {
            _diagnostics?.AddError(
                string.Empty,
                new SourceSpan(string.Empty, startLine, startColumn, _line, _column),
                "GG0003",
                "未闭合的字符串字面量");
        }
        else
        {
            Advance();
        }

        return new Token(TokenType.String, sb.ToString(), startLine, startColumn);
    }

    private Token ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X'))
        {
            Advance();
            Advance();
            sb.Append("0x");

            while (!IsAtEnd() && IsHexDigit(Peek()))
            {
                sb.Append(Advance());
            }

            return new Token(TokenType.Number, sb.ToString(), startLine, startColumn);
        }

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F' || Peek() == 'i' || Peek() == 'I'
            || Peek() == 'u' || Peek() == 'U'))
        {
            sb.Append(Advance());
        }

        return new Token(TokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    private Token ScanIdentifierOrKeyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek())))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();

        if (Keywords.Contains(text))
        {
            if (text == "true" || text == "false" || text == "null")
            {
                return new Token(TokenType.Literal, text, startLine, startColumn);
            }

            return new Token(TokenType.Keyword, text, startLine, startColumn);
        }

        if (TypeKeywords.Contains(text))
        {
            return new Token(TokenType.TypeKeyword, text, startLine, startColumn);
        }

        return new Token(TokenType.Identifier, text, startLine, startColumn);
    }

    private Token ScanAttribute(int startLine, int startColumn)
    {
        Advance();

        var sb = new StringBuilder("[");

        while (!IsAtEnd() && Peek() != ']')
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd())
        {
            sb.Append(Advance());
        }

        return new Token(TokenType.Attribute, sb.ToString(), startLine, startColumn);
    }

    private bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&'
            or '|' or '^' or '~' or '?' or ':' => true,
            _ => false
        };
    }

    private Token ScanOperator(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();

            if (Operators.Contains(candidate))
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }

        var op = sb.ToString();

        if (op == ":" || op == "::")
        {
            return new Token(TokenType.Punctuation, op, startLine, startColumn);
        }

        return new Token(TokenType.Operator, op, startLine, startColumn);
    }

    #endregion
}
