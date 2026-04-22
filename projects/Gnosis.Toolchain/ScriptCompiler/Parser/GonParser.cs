using System.Text;
using Gnosis.Core.Diagnostic;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

public class GonParser
{
    #region Fields

    private string _source = string.Empty;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private DiagnosticSink? _diagnostics;

    #endregion

    #region Constructors

    public GonParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public GonValue Parse(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;

        SkipWhitespace();
        return ParseValue();
    }

    #endregion

    #region Private Methods

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _source[_position];
    }

    private char Advance()
    {
        var c = _source[_position++];
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

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            Advance();
        }
    }

    private void SkipComment()
    {
        if (Peek() == '#')
        {
            if (_position + 1 < _source.Length && _source[_position + 1] == '>')
            {
                return;
            }

            while (!IsAtEnd() && Peek() != '\n')
            {
                Advance();
            }
        }
        else if (Peek() == '<' && _position + 1 < _source.Length && _source[_position + 1] == '#')
        {
            Advance();
            Advance();

            var depth = 1;

            while (!IsAtEnd() && depth > 0)
            {
                if (Peek() == '<' && _position + 1 < _source.Length && _source[_position + 1] == '#')
                {
                    Advance();
                    Advance();
                    depth++;
                }
                else if (Peek() == '#' && _position + 1 < _source.Length && _source[_position + 1] == '>')
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
        }
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            if (char.IsWhiteSpace(Peek()))
            {
                SkipWhitespace();
            }
            else if (Peek() == '#' || Peek() == '<')
            {
                SkipComment();
            }
            else
            {
                break;
            }
        }
    }

    private GonValue ParseValue()
    {
        SkipWhitespaceAndComments();

        if (IsAtEnd())
        {
            return GonValue.Null();
        }

        var c = Peek();

        if (c == '{')
        {
            return ParseObject(null, null);
        }

        if (c == '[')
        {
            return ParseArray();
        }

        if (c == '"')
        {
            return GonValue.String(ParseQuotedString());
        }

        if (c == '-' || char.IsDigit(c))
        {
            return ParseNumber();
        }

        if (IsIdentifierStart(c))
        {
            return ParseIdentifierValue();
        }

        _diagnostics?.AddError(
            string.Empty,
            new SourceSpan(string.Empty, _line, _column, _line, _column),
            "GG2001",
            $"意外的字符 '{c}'");

        Advance();
        return GonValue.Null();
    }

    private GonValue ParseObject(string? typeName, string? variantName)
    {
        Advance();

        var fields = new Dictionary<string, GonValue>();

        SkipWhitespaceAndComments();

        if (Peek() != '}')
        {
            ParseField(fields);

            while (true)
            {
                SkipWhitespaceAndComments();

                if (Peek() != ',')
                {
                    break;
                }

                Advance();
                SkipWhitespaceAndComments();

                if (Peek() == '}')
                {
                    break;
                }

                ParseField(fields);
            }
        }

        SkipWhitespaceAndComments();

        if (Peek() == '}')
        {
            Advance();
        }
        else
        {
            _diagnostics?.AddError(
                string.Empty,
                new SourceSpan(string.Empty, _line, _column, _line, _column),
                "GG2002",
                "期望 '}'");
        }

        return GonValue.Object(typeName, variantName, fields);
    }

    private void ParseField(Dictionary<string, GonValue> fields)
    {
        SkipWhitespaceAndComments();

        string fieldName;

        if (Peek() == '"')
        {
            fieldName = ParseQuotedString();
        }
        else
        {
            fieldName = ParseIdentifier();
        }

        SkipWhitespaceAndComments();

        if (Peek() == ':')
        {
            Advance();
        }
        else
        {
            _diagnostics?.AddError(
                string.Empty,
                new SourceSpan(string.Empty, _line, _column, _line, _column),
                "GG2003",
                $"期望 ':'，但遇到 '{Peek()}'");
        }

        var value = ParseValue();
        fields[fieldName] = value;
    }

    private GonValue ParseArray()
    {
        Advance();

        var elements = new List<GonValue>();

        SkipWhitespaceAndComments();

        if (Peek() != ']')
        {
            elements.Add(ParseValue());

            while (true)
            {
                SkipWhitespaceAndComments();

                if (Peek() != ',')
                {
                    break;
                }

                Advance();
                SkipWhitespaceAndComments();

                if (Peek() == ']')
                {
                    break;
                }

                elements.Add(ParseValue());
            }
        }

        SkipWhitespaceAndComments();

        if (Peek() == ']')
        {
            Advance();
        }
        else
        {
            _diagnostics?.AddError(
                string.Empty,
                new SourceSpan(string.Empty, _line, _column, _line, _column),
                "GG2004",
                "期望 ']'");
        }

        return GonValue.Array(elements);
    }

    private GonValue ParseNumber()
    {
        var start = _position;
        var isNegative = false;

        if (Peek() == '-')
        {
            isNegative = true;
            Advance();
        }

        var sb = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.' && _position + 1 < _source.Length && char.IsDigit(_source[_position + 1]))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }

            var doubleVal = double.Parse(sb.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            if (isNegative)
            {
                doubleVal = -doubleVal;
            }

            if (!IsAtEnd() && Peek() == 'f')
            {
                Advance();
                return GonValue.Float((float)doubleVal);
            }

            return GonValue.Double(doubleVal);
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

            var doubleVal = double.Parse(sb.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            if (isNegative)
            {
                doubleVal = -doubleVal;
            }

            return GonValue.Double(doubleVal);
        }

        if (!IsAtEnd() && Peek() == 'u')
        {
            Advance();
            var ulongVal = ulong.Parse(sb.ToString());
            if (isNegative)
            {
                _diagnostics?.AddWarning(
                    string.Empty,
                    new SourceSpan(string.Empty, _line, _column, _line, _column),
                    "GG2005",
                    "无符号整数不能为负数");
            }
            return GonValue.UnsignedInteger(ulongVal);
        }

        var longVal = long.Parse(sb.ToString());
        if (isNegative)
        {
            longVal = -longVal;
        }

        return GonValue.Integer(longVal);
    }

    private GonValue ParseIdentifierValue()
    {
        var identifier = ParseIdentifier();

        SkipWhitespaceAndComments();

        if (identifier == "true")
        {
            return GonValue.Boolean(true);
        }

        if (identifier == "false")
        {
            return GonValue.Boolean(false);
        }

        if (identifier == "null")
        {
            return GonValue.Null();
        }

        var typeName = identifier;

        SkipWhitespaceAndComments();

        var nextChar = Peek();

        if (IsIdentifierStart(nextChar))
        {
            var variantName = ParseIdentifier();
            SkipWhitespaceAndComments();
            nextChar = Peek();

            if (nextChar == '{')
            {
                return ParseObject(typeName, variantName);
            }

            _diagnostics?.AddError(
                string.Empty,
                new SourceSpan(string.Empty, _line, _column, _line, _column),
                "GG2006",
                "变体名后期望 '{'");

            return GonValue.Null();
        }

        if (nextChar == '{')
        {
            return ParseObject(typeName, null);
        }

        _diagnostics?.AddError(
            string.Empty,
            new SourceSpan(string.Empty, _line, _column, _line, _column),
            "GG2007",
            $"标识符 '{identifier}' 不是有效的值");

        return GonValue.Null();
    }

    private string ParseIdentifier()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private string ParseQuotedString()
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
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
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd())
        {
            Advance();
        }

        return sb.ToString();
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    #endregion
}
