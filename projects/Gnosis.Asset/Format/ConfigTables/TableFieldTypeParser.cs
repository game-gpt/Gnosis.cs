using System.Globalization;
using System.Text;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed class TableFieldTypeParser
{
    private string _source = string.Empty;
    private int _position;

    public TableFieldType Parse(string typeString)
    {
        _source = typeString.Trim();
        _position = 0;

        return ParseType();
    }

    private TableFieldType ParseType()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            throw new FormatException($"类型字符串为空");
        }

        if (Peek() == '&')
        {
            return ParseReferenceType();
        }

        if (Peek() == '[')
        {
            return ParseListType();
        }

        return ParsePrimitiveType();
    }

    private TableFieldType ParsePrimitiveType()
    {
        var identifier = ReadIdentifier();

        return identifier switch
        {
            "i32" => TableFieldType.I32,
            "i64" => TableFieldType.I64,
            "f32" => TableFieldType.F32,
            "f64" => TableFieldType.F64,
            "bool" => TableFieldType.Bool,
            "string" => TableFieldType.String,
            _ => throw new FormatException($"未知的基础类型：{identifier}")
        };
    }

    private TableFieldType ParseReferenceType()
    {
        Advance();

        var targetTable = ReadIdentifier();

        if (string.IsNullOrEmpty(targetTable))
        {
            throw new FormatException("引用类型 & 后缺少表名");
        }

        return TableFieldType.Reference(targetTable);
    }

    private TableFieldType ParseListType()
    {
        Advance();

        SkipWhitespace();

        var elementType = ParseType();

        SkipWhitespace();

        if (Peek() == ';')
        {
            Advance();
            SkipWhitespace();

            var sizeStr = ReadWhile(char.IsDigit);

            if (!int.TryParse(sizeStr, NumberStyles.None, CultureInfo.InvariantCulture, out var size))
            {
                throw new FormatException($"无效的固定数组大小：{sizeStr}");
            }

            SkipWhitespace();

            if (Peek() != ']')
            {
                throw new FormatException("固定数组类型期望 ']' 结尾");
            }

            Advance();

            return TableFieldType.FixedList(elementType, size);
        }

        if (Peek() != ']')
        {
            throw new FormatException("列表类型期望 ']' 结尾");
        }

        Advance();

        return TableFieldType.List(elementType);
    }

    private string ReadIdentifier()
    {
        SkipWhitespace();

        var sb = new StringBuilder();

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private string ReadWhile(Func<char, bool> predicate)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && predicate(Peek()))
        {
            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            Advance();
        }
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    private char Advance() => _source[_position++];

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
}
