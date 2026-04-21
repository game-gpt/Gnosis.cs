namespace Gnosis.Assets.Formats.ConfigTables;

public sealed class ForeignKeyCheckResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ForeignKeyError> Errors { get; init; } = [];
}

public sealed record ForeignKeyError
{
    public string SourceTable { get; init; } = string.Empty;
    public string SourceField { get; init; } = string.Empty;
    public int RowIndex { get; init; }
    public object? InvalidValue { get; init; }
    public string TargetTable { get; init; } = string.Empty;
}

public sealed class ConfigTableForeignKeyChecker
{
    public ForeignKeyCheckResult Check(IReadOnlyDictionary<string, ConfigTableData> tables)
    {
        var errors = new List<ForeignKeyError>();

        foreach (var (tableName, tableData) in tables)
        {
            var refFields = tableData.Schema.Fields
                .Where(f => f.Type.Kind == TableFieldTypeKind.Reference)
                .ToList();

            if (refFields.Count == 0)
            {
                continue;
            }

            foreach (var field in refFields)
            {
                var targetTableName = field.Type.GetReferenceTarget()!;

                if (!tables.TryGetValue(targetTableName, out var targetTable))
                {
                    for (var i = 0; i < tableData.Rows.Count; i++)
                    {
                        var value = tableData.Rows[i].GetValue(tableData.Schema, field.Name);

                        if (value is not null)
                        {
                            errors.Add(new ForeignKeyError
                            {
                                SourceTable = tableName,
                                SourceField = field.Name,
                                RowIndex = i,
                                InvalidValue = value,
                                TargetTable = targetTableName
                            });
                        }
                    }

                    continue;
                }

                var targetPrimaryKey = targetTable.Schema.GetPrimaryKeyField();

                if (targetPrimaryKey is null)
                {
                    continue;
                }

                var validKeys = new HashSet<object?>();

                foreach (var row in targetTable.Rows)
                {
                    var key = row.GetValue(targetTable.Schema, targetPrimaryKey.Name);
                    validKeys.Add(key);
                }

                for (var i = 0; i < tableData.Rows.Count; i++)
                {
                    var value = tableData.Rows[i].GetValue(tableData.Schema, field.Name);

                    if (value is null)
                    {
                        continue;
                    }

                    if (value is IList<object?> listValue)
                    {
                        foreach (var item in listValue)
                        {
                            if (!validKeys.Contains(item))
                            {
                                errors.Add(new ForeignKeyError
                                {
                                    SourceTable = tableName,
                                    SourceField = field.Name,
                                    RowIndex = i,
                                    InvalidValue = item,
                                    TargetTable = targetTableName
                                });
                            }
                        }
                    }
                    else if (!validKeys.Contains(value))
                    {
                        errors.Add(new ForeignKeyError
                        {
                            SourceTable = tableName,
                            SourceField = field.Name,
                            RowIndex = i,
                            InvalidValue = value,
                            TargetTable = targetTableName
                        });
                    }
                }
            }
        }

        return new ForeignKeyCheckResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
