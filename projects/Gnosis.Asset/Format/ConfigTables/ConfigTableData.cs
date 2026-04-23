using Oak.Csv;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed record TableField
{
    public string Name { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public TableFieldType Type { get; init; } = TableFieldType.String;
    public bool IsPrimaryKey { get; init; }
}

public sealed record TableSchema
{
    public string TableName { get; init; } = string.Empty;
    public IReadOnlyList<TableField> Fields { get; init; } = [];
    public string? PrimaryKeyFieldName { get; init; }

    public TableField? GetPrimaryKeyField()
    {
        return Fields.FirstOrDefault(f => f.IsPrimaryKey);
    }

    public TableField? GetField(string name)
    {
        return Fields.FirstOrDefault(f => f.Name == name);
    }
}

public sealed record TableRow
{
    public IReadOnlyList<object?> Values { get; init; } = [];

    public object? GetValue(TableSchema schema, string fieldName)
    {
        var index = schema.Fields.ToList().FindIndex(f => f.Name == fieldName);
        return index >= 0 && index < Values.Count ? Values[index] : null;
    }
}

public sealed record ConfigTableData
{
    public string TableName { get; init; } = string.Empty;
    public TableSchema Schema { get; init; } = new();
    public IReadOnlyList<TableRow> Rows { get; init; } = [];
    public string SourcePath { get; init; } = string.Empty;
}
