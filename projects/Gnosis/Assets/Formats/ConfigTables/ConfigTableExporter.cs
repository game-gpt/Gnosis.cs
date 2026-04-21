using System.Globalization;
using System.Text;

namespace Gnosis.Assets.Formats.ConfigTables;

public sealed class ConfigTableExporter
{
    private readonly ConfigTableCodeGenerator _codeGenerator = new();

    public async Task ExportAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDir);

        await ExportScriptAsync(tableData, outputDir, cancellationToken);
        await ExportGonAsync(tableData, outputDir, cancellationToken);
        await ExportBinaryAsync(tableData, outputDir, cancellationToken);
    }

    private async Task ExportScriptAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken)
    {
        var code = _codeGenerator.GenerateScriptCode(tableData);
        var scriptPath = Path.Combine(outputDir, $"{tableData.TableName}Table.script");

        await File.WriteAllTextAsync(scriptPath, code, cancellationToken);
    }

    private async Task ExportGonAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken)
    {
        var gonContent = GenerateGonContent(tableData);
        var gonPath = Path.Combine(outputDir, $"{tableData.TableName}Table.gon");

        await File.WriteAllTextAsync(gonPath, gonContent, cancellationToken);
    }

    private async Task ExportBinaryAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken)
    {
        var binaryData = SerializeToBinary(tableData);
        var binPath = Path.Combine(outputDir, $"{tableData.TableName}Table.bin");

        await File.WriteAllBytesAsync(binPath, binaryData, cancellationToken);
    }

    private static string GenerateGonContent(ConfigTableData tableData)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"// 配置表: {tableData.TableName}");
        sb.AppendLine($"// 来源: {tableData.SourcePath}");
        sb.AppendLine();

        foreach (var row in tableData.Rows)
        {
            sb.Append($"{tableData.TableName}Row {{ ");

            for (var i = 0; i < tableData.Schema.Fields.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var field = tableData.Schema.Fields[i];
                var value = i < row.Values.Count ? row.Values[i] : null;

                sb.Append($"{field.Name}: {FormatGonValue(value, field.Type)}");
            }

            sb.AppendLine(" }");
        }

        return sb.ToString();
    }

    private static string FormatGonValue(object? value, TableFieldType type)
    {
        if (value is null)
        {
            return "null";
        }

        return type.Kind switch
        {
            TableFieldTypeKind.I32 => Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            TableFieldTypeKind.I64 => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            TableFieldTypeKind.F32 => $"{Convert.ToSingle(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}f",
            TableFieldTypeKind.F64 => Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            TableFieldTypeKind.Bool => (bool)value ? "true" : "false",
            TableFieldTypeKind.String => $"\"{value}\"",
            TableFieldTypeKind.List or TableFieldTypeKind.FixedList => FormatGonList((IList<object?>)value, type.GetListElementType()!),
            TableFieldTypeKind.Reference => value.ToString() ?? "null",
            _ => value.ToString() ?? "null"
        };
    }

    private static string FormatGonList(IList<object?> list, TableFieldType elementType)
    {
        if (list.Count == 0)
        {
            return "[]";
        }

        var sb = new StringBuilder("[ ");

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(FormatGonValue(list[i], elementType));
        }

        sb.Append(" ]");

        return sb.ToString();
    }

    private static byte[] SerializeToBinary(ConfigTableData tableData)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(new byte[] { 0x47, 0x47, 0x54, 0x42 });

        writer.Write(tableData.TableName);

        writer.Write(tableData.Schema.Fields.Count);

        foreach (var field in tableData.Schema.Fields)
        {
            writer.Write(field.Name);
            writer.Write((byte)field.Type.Kind);
        }

        writer.Write(tableData.Rows.Count);

        foreach (var row in tableData.Rows)
        {
            for (var i = 0; i < tableData.Schema.Fields.Count; i++)
            {
                var value = i < row.Values.Count ? row.Values[i] : null;
                var field = tableData.Schema.Fields[i];

                WriteBinaryValue(writer, value, field.Type);
            }
        }

        return ms.ToArray();
    }

    private static void WriteBinaryValue(BinaryWriter writer, object? value, TableFieldType type)
    {
        if (value is null)
        {
            writer.Write((byte)0);
            return;
        }

        writer.Write((byte)1);

        switch (type.Kind)
        {
            case TableFieldTypeKind.I32:
                writer.Write(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                break;
            case TableFieldTypeKind.I64:
                writer.Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case TableFieldTypeKind.F32:
                writer.Write(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                break;
            case TableFieldTypeKind.F64:
                writer.Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case TableFieldTypeKind.Bool:
                writer.Write((bool)value);
                break;
            case TableFieldTypeKind.String:
                writer.Write((string)value);
                break;
            case TableFieldTypeKind.List:
            case TableFieldTypeKind.FixedList:
                WriteBinaryList(writer, (IList<object?>)value, type.GetListElementType()!);
                break;
            case TableFieldTypeKind.Reference:
                WriteBinaryReference(writer, value);
                break;
            default:
                writer.Write(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static void WriteBinaryList(BinaryWriter writer, IList<object?> list, TableFieldType elementType)
    {
        writer.Write(list.Count);

        foreach (var item in list)
        {
            WriteBinaryValue(writer, item, elementType);
        }
    }

    private static void WriteBinaryReference(BinaryWriter writer, object value)
    {
        if (value is int intVal)
        {
            writer.Write(intVal);
        }
        else if (value is long longVal)
        {
            writer.Write(longVal);
        }
        else
        {
            writer.Write(value.ToString() ?? string.Empty);
        }
    }
}
