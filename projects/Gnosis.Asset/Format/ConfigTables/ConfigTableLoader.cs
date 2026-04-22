using System.Globalization;
using System.Text;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed class ConfigTableLoader
{
    public ConfigTableData LoadFromBinary(string path)
    {
        var bytes = File.ReadAllBytes(path);

        return DeserializeFromBinary(bytes, path);
    }

    public async Task<ConfigTableData> LoadFromBinaryAsync(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);

        return DeserializeFromBinary(bytes, path);
    }

    private static ConfigTableData DeserializeFromBinary(byte[] data, string path)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var magic = reader.ReadBytes(4);

        if (magic[0] != 0x47 || magic[1] != 0x47 || magic[2] != 0x54 || magic[3] != 0x42)
        {
            throw new FormatException($"无效的配置表二进制文件：{path}，魔数不匹配");
        }

        var tableName = reader.ReadString();
        var fieldCount = reader.ReadInt32();
        var fields = new List<TableField>();

        for (var i = 0; i < fieldCount; i++)
        {
            var name = reader.ReadString();
            var kind = (TableFieldTypeKind)reader.ReadByte();
            var type = KindToFieldType(kind);

            fields.Add(new TableField
            {
                Name = name,
                Type = type,
                IsPrimaryKey = name.Equals("id", StringComparison.OrdinalIgnoreCase)
            });
        }

        var rowCount = reader.ReadInt32();
        var rows = new List<TableRow>();

        for (var r = 0; r < rowCount; r++)
        {
            var values = new object?[fieldCount];

            for (var i = 0; i < fieldCount; i++)
            {
                values[i] = ReadBinaryValue(reader, fields[i].Type);
            }

            rows.Add(new TableRow { Values = values });
        }

        var primaryKeyField = fields.FirstOrDefault(f => f.IsPrimaryKey);

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
            SourcePath = path
        };
    }

    private static TableFieldType KindToFieldType(TableFieldTypeKind kind)
    {
        return kind switch
        {
            TableFieldTypeKind.I32 => TableFieldType.I32,
            TableFieldTypeKind.I64 => TableFieldType.I64,
            TableFieldTypeKind.F32 => TableFieldType.F32,
            TableFieldTypeKind.F64 => TableFieldType.F64,
            TableFieldTypeKind.Bool => TableFieldType.Bool,
            TableFieldTypeKind.String => TableFieldType.String,
            TableFieldTypeKind.List => TableFieldType.List(TableFieldType.String),
            TableFieldTypeKind.FixedList => TableFieldType.FixedList(TableFieldType.String, 0),
            TableFieldTypeKind.Reference => TableFieldType.Reference("Unknown"),
            _ => TableFieldType.String
        };
    }

    private static object? ReadBinaryValue(BinaryReader reader, TableFieldType type)
    {
        var hasValue = reader.ReadByte();

        if (hasValue == 0)
        {
            return null;
        }

        return type.Kind switch
        {
            TableFieldTypeKind.I32 => reader.ReadInt32(),
            TableFieldTypeKind.I64 => reader.ReadInt64(),
            TableFieldTypeKind.F32 => reader.ReadSingle(),
            TableFieldTypeKind.F64 => reader.ReadDouble(),
            TableFieldTypeKind.Bool => reader.ReadBoolean(),
            TableFieldTypeKind.String => reader.ReadString(),
            TableFieldTypeKind.List => ReadBinaryList(reader, type.GetListElementType()!),
            TableFieldTypeKind.FixedList => ReadBinaryList(reader, type.GetListElementType()!),
            TableFieldTypeKind.Reference => ReadBinaryReference(reader),
            _ => reader.ReadString()
        };
    }

    private static List<object?> ReadBinaryList(BinaryReader reader, TableFieldType elementType)
    {
        var count = reader.ReadInt32();
        var list = new List<object?>(count);

        for (var i = 0; i < count; i++)
        {
            list.Add(ReadBinaryValue(reader, elementType));
        }

        return list;
    }

    private static object ReadBinaryReference(BinaryReader reader)
    {
        return reader.ReadInt32();
    }
}
