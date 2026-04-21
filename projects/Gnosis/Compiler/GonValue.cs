using System.Text;

namespace Gnosis.Compiler;

public sealed class GonValue
{
    public GonValueType Type { get; }
    public object? RawValue { get; }
    public string? TypeName { get; }
    public string? VariantName { get; }
    public Dictionary<string, GonValue>? Fields { get; }
    public List<GonValue>? Elements { get; }

    private GonValue(GonValueType type, object? rawValue = null, string? typeName = null,
        string? variantName = null, Dictionary<string, GonValue>? fields = null, List<GonValue>? elements = null)
    {
        Type = type;
        RawValue = rawValue;
        TypeName = typeName;
        VariantName = variantName;
        Fields = fields;
        Elements = elements;
    }

    public static GonValue Null() => new(GonValueType.Null);
    public static GonValue Boolean(bool value) => new(GonValueType.Boolean, value);
    public static GonValue Integer(long value) => new(GonValueType.Integer, value);
    public static GonValue UnsignedInteger(ulong value) => new(GonValueType.UnsignedInteger, value);
    public static GonValue Float(float value) => new(GonValueType.Float, value);
    public static GonValue Double(double value) => new(GonValueType.Double, value);
    public static GonValue String(string value) => new(GonValueType.String, value);

    public static GonValue Object(string? typeName, string? variantName, Dictionary<string, GonValue> fields)
        => new(GonValueType.Object, typeName: typeName, variantName: variantName, fields: fields);

    public static GonValue Array(List<GonValue> elements)
        => new(GonValueType.Array, elements: elements);

    public bool GetBoolean() => Type == GonValueType.Boolean && (bool)(RawValue ?? false);
    public long GetInteger() => Type == GonValueType.Integer ? (long)(RawValue ?? 0L) : 0L;
    public float GetFloat() => Type == GonValueType.Float ? (float)(RawValue ?? 0f) : 0f;
    public string? GetString() => Type == GonValueType.String ? (string?)RawValue : null;

    public GonValue? GetField(string name)
    {
        if (Fields is not null && Fields.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    public T? GetFieldAs<T>(string name) where T : struct
    {
        var field = GetField(name);
        if (field is null)
        {
            return null;
        }

        return field.Type switch
        {
            GonValueType.Integer => (T)(object)field.GetInteger(),
            GonValueType.Float => (T)(object)field.GetFloat(),
            GonValueType.Boolean => (T)(object)field.GetBoolean(),
            _ => null
        };
    }

    public override string ToString()
    {
        return Type switch
        {
            GonValueType.Null => "null",
            GonValueType.Boolean => GetBoolean() ? "true" : "false",
            GonValueType.Integer => GetInteger().ToString(),
            GonValueType.UnsignedInteger => RawValue?.ToString() ?? "0",
            GonValueType.Float => GetFloat().ToString("F"),
            GonValueType.Double => RawValue?.ToString() ?? "0",
            GonValueType.String => $"\"{GetString()}\"",
            GonValueType.Object => FormatObject(),
            GonValueType.Array => FormatArray(),
            _ => "unknown"
        };
    }

    private string FormatObject()
    {
        var sb = new StringBuilder();

        if (TypeName is not null)
        {
            sb.Append(TypeName);
            sb.Append(' ');
        }

        if (VariantName is not null)
        {
            sb.Append(VariantName);
            sb.Append(' ');
        }

        sb.Append("{ ");

        if (Fields is not null)
        {
            var first = true;
            foreach (var (key, value) in Fields)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(key);
                sb.Append(": ");
                sb.Append(value.ToString());
                first = false;
            }
        }

        sb.Append(" }");
        return sb.ToString();
    }

    private string FormatArray()
    {
        if (Elements is null || Elements.Count == 0)
        {
            return "[]";
        }

        var sb = new StringBuilder("[ ");

        for (var i = 0; i < Elements.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(Elements[i].ToString());
        }

        sb.Append(" ]");
        return sb.ToString();
    }
}