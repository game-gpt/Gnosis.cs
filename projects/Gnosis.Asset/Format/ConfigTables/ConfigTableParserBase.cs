using System.Globalization;
using System.Text;
using Oak.Csv;

namespace Gnosis.Asset.Format.ConfigTables;

public abstract class ConfigTableParserBase
{
    private readonly TableFieldTypeParser _typeParser = new();

    protected ConfigTableMapping Mapping { get; }

    protected ConfigTableParserBase(ConfigTableMapping? mapping = null)
    {
        Mapping = mapping ?? new ConfigTableMapping();
    }

    public abstract Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default);

    protected ConfigTableData BuildTableData(string tableName, IReadOnlyList<IReadOnlyList<string>> rawRows, string sourcePath)
    {
        var commentRow = GetRow(rawRows, Mapping.CommentRow);
        var fieldNameRow = GetRow(rawRows, Mapping.FieldNameRow);
        var fieldTypeRow = GetRow(rawRows, Mapping.FieldTypeRow);

        var columnCount = fieldNameRow.Count;
        var fields = new List<TableField>();

        for (var i = 0; i < columnCount; i++)
        {
            var name = i < fieldNameRow.Count ? fieldNameRow[i].Trim() : $"field_{i}";
            var comment = i < commentRow.Count ? commentRow[i].Trim() : string.Empty;
            var typeStr = i < fieldTypeRow.Count ? fieldTypeRow[i].Trim() : "string";

            var fieldType = _typeParser.Parse(typeStr);
            var isPrimaryKey = name.Equals("id", StringComparison.OrdinalIgnoreCase);

            fields.Add(new TableField
            {
                Name = name,
                Comment = comment,
                Type = fieldType,
                IsPrimaryKey = isPrimaryKey
            });
        }

        var primaryKeyField = fields.FirstOrDefault(f => f.IsPrimaryKey);

        var rows = new List<TableRow>();
        for (var r = Mapping.DataStartRow - 1; r < rawRows.Count; r++)
        {
            var rawRow = rawRows[r];

            if (IsEmptyRow(rawRow))
            {
                continue;
            }

            var values = new object?[columnCount];

            for (var c = 0; c < columnCount; c++)
            {
                var rawValue = c < rawRow.Count ? rawRow[c].Trim() : string.Empty;
                values[c] = ParseValue(rawValue, fields[c].Type);
            }

            rows.Add(new TableRow { Values = values });
        }

        var schema = new TableSchema
        {
            TableName = tableName,
            Fields = fields,
            PrimaryKeyFieldName = primaryKeyField?.Name
        };

        return new ConfigTableData
        {
            TableName = tableName,
            Schema = schema,
            Rows = rows,
            SourcePath = sourcePath
        };
    }

    private static IReadOnlyList<string> GetRow(IReadOnlyList<IReadOnlyList<string>> rawRows, int rowIndex)
    {
        var idx = rowIndex - 1;
        return idx >= 0 && idx < rawRows.Count ? rawRows[idx] : [];
    }

    private static bool IsEmptyRow(IReadOnlyList<string> row)
    {
        return row.All(cell => string.IsNullOrWhiteSpace(cell));
    }

    private static object? ParseValue(string raw, TableFieldType type)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "null")
        {
            return null;
        }

        return type.Kind switch
        {
            TableFieldTypeKind.I32 => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32) ? i32 : 0,
            TableFieldTypeKind.I64 => long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64) ? i64 : 0L,
            TableFieldTypeKind.F32 => float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f32) ? f32 : 0f,
            TableFieldTypeKind.F64 => double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f64) ? f64 : 0.0,
            TableFieldTypeKind.Bool => ParseBool(raw),
            TableFieldTypeKind.String => raw,
            TableFieldTypeKind.List => ParseListValue(raw, type.GetListElementType()!),
            TableFieldTypeKind.FixedList => ParseListValue(raw, type.GetListElementType()!),
            TableFieldTypeKind.Reference => ParseReferenceValue(raw),
            _ => raw
        };
    }

    private static bool ParseBool(string raw)
    {
        return raw.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            _ => false
        };
    }

    private static List<object?> ParseListValue(string raw, TableFieldType elementType)
    {
        var trimmed = raw.Trim();

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var parts = SplitListElements(trimmed);
        var result = new List<object?>();

        foreach (var part in parts)
        {
            result.Add(ParseValue(part.Trim(), elementType));
        }

        return result;
    }

    private static List<string> SplitListElements(string content)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var depth = 0;

        foreach (var c in content)
        {
            if (c == '[')
            {
                depth++;
                sb.Append(c);
            }
            else if (c == ']')
            {
                depth--;
                sb.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result;
    }

    private static object? ParseReferenceValue(string raw)
    {
        var trimmed = raw.Trim();

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            return ParseListValue(trimmed, TableFieldType.I32);
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return id;
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longId))
        {
            return longId;
        }

        return trimmed;
    }
}
