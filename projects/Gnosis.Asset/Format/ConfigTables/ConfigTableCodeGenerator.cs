using System.Text;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed class ConfigTableCodeGenerator
{
    public string GenerateScriptCode(ConfigTableData tableData)
    {
        var sb = new StringBuilder();
        var className = $"{tableData.TableName}Table";

        sb.AppendLine($"export class {className}");
        sb.AppendLine("{");

        GenerateRowRecord(sb, tableData);
        sb.AppendLine();

        GenerateFields(sb, tableData);
        sb.AppendLine();

        GenerateLoadMethod(sb, className, tableData);
        sb.AppendLine();

        GenerateGetByIdMethod(sb, tableData);
        sb.AppendLine();

        GenerateGetAllMethod(sb);
        sb.AppendLine();

        GenerateFindByMethod(sb, tableData);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateRowRecord(StringBuilder sb, ConfigTableData tableData)
    {
        var rowName = $"{tableData.TableName}Row";

        sb.AppendLine($"    export record {rowName}");
        sb.AppendLine("    {");

        foreach (var field in tableData.Schema.Fields)
        {
            var ggType = MapToGgScriptType(field.Type);
            sb.AppendLine($"        {field.Name}: {ggType}");
        }

        sb.AppendLine("    }");
    }

    private static void GenerateFields(StringBuilder sb, ConfigTableData tableData)
    {
        var rowName = $"{tableData.TableName}Row";

        sb.AppendLine($"    var _rows: [{rowName}]");
        sb.AppendLine($"    var _idMap: Map<i32, {rowName}>");
    }

    private static void GenerateLoadMethod(StringBuilder sb, string className, ConfigTableData tableData)
    {
        var rowName = $"{tableData.TableName}Row";

        sb.AppendLine($"    static func load() -> {className}");
        sb.AppendLine("    {");
        sb.AppendLine($"        var table = {className}()");
        sb.AppendLine($"        table._rows = config_table.load(\"{tableData.TableName}\") as [{rowName}]");

        if (tableData.Schema.PrimaryKeyFieldName is not null)
        {
            sb.AppendLine("        table._idMap = Map<i32, {rowName}>()");
            sb.AppendLine("        loop row in table._rows");
            sb.AppendLine("        {");
            sb.AppendLine($"            table._idMap.set(row.{tableData.Schema.PrimaryKeyFieldName}, row)");
            sb.AppendLine("        }");
        }

        sb.AppendLine("        return table");
        sb.AppendLine("    }");
    }

    private static void GenerateGetByIdMethod(StringBuilder sb, ConfigTableData tableData)
    {
        if (tableData.Schema.PrimaryKeyFieldName is null)
        {
            return;
        }

        var rowName = $"{tableData.TableName}Row";

        sb.AppendLine($"    func get(id: i32) -> {rowName}?");
        sb.AppendLine("    {");
        sb.AppendLine("        return _idMap.get(id)");
        sb.AppendLine("    }");
    }

    private static void GenerateGetAllMethod(StringBuilder sb)
    {
        sb.AppendLine("    func getAll() -> [_rows.Element]");
        sb.AppendLine("    {");
        sb.AppendLine("        return _rows");
        sb.AppendLine("    }");
    }

    private static void GenerateFindByMethod(StringBuilder sb, ConfigTableData tableData)
    {
        var rowName = $"{tableData.TableName}Row";

        foreach (var field in tableData.Schema.Fields)
        {
            if (field.IsPrimaryKey)
            {
                continue;
            }

            if (field.Type.Kind is not (TableFieldTypeKind.I32 or TableFieldTypeKind.I64
                or TableFieldTypeKind.F32 or TableFieldTypeKind.F64
                or TableFieldTypeKind.Bool or TableFieldTypeKind.String))
            {
                continue;
            }

            var ggType = MapToGgScriptType(field.Type);
            var methodName = $"findBy{char.ToUpperInvariant(field.Name[0])}{field.Name[1..]}";

            sb.AppendLine($"    func {methodName}(value: {ggType}) -> [{rowName}]");
            sb.AppendLine("    {");
            sb.AppendLine("        var result: [{rowName}] = []");
            sb.AppendLine("        loop row in _rows");
            sb.AppendLine("        {");
            sb.AppendLine($"            if row.{field.Name} == value");
            sb.AppendLine("            {");
            sb.AppendLine("                result.push(row)");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        return result");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static string MapToGgScriptType(TableFieldType type)
    {
        return type.Kind switch
        {
            TableFieldTypeKind.I32 => "i32",
            TableFieldTypeKind.I64 => "i64",
            TableFieldTypeKind.F32 => "f32",
            TableFieldTypeKind.F64 => "f64",
            TableFieldTypeKind.Bool => "bool",
            TableFieldTypeKind.String => "string",
            TableFieldTypeKind.List => $"[{MapToGgScriptType(type.GetListElementType()!)}]",
            TableFieldTypeKind.FixedList => $"[{MapToGgScriptType(type.GetListElementType()!)}; {type.GetFixedListSize()}]",
            TableFieldTypeKind.Reference => "i32",
            _ => "string"
        };
    }
}
