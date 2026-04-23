using Gnosis.Toolchain.ScriptCompiler.Diagnostics;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     GON 配置格式解析器（基于 Oak.Gon 的适配层）
/// </summary>
public sealed class GonParser
{
    private readonly DiagnosticSink? _diagnostics;

    public GonParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析 GON 文本
    /// </summary>
    public GonValue Parse(string source)
    {
        var oakParser = new Oak.Gon.GonParser(_diagnostics == null ? null : new Oak.Core.Diagnostics.DiagnosticSink());
        var oakValue = oakParser.Parse(source);
        return ConvertValue(oakValue);
    }

    private static GonValue ConvertValue(Oak.Gon.GonValue oakValue)
    {
        return oakValue.Type switch
        {
            Oak.Gon.GonValueType.Null => GonValue.Null(),
            Oak.Gon.GonValueType.Boolean => GonValue.Boolean(oakValue.GetBoolean()),
            Oak.Gon.GonValueType.Integer => GonValue.Integer(oakValue.GetInteger()),
            Oak.Gon.GonValueType.UnsignedInteger => GonValue.UnsignedInteger((ulong)(oakValue.RawValue ?? 0UL)),
            Oak.Gon.GonValueType.Float => GonValue.Float(oakValue.GetFloat()),
            Oak.Gon.GonValueType.Double => GonValue.Double((double)(oakValue.RawValue ?? 0.0)),
            Oak.Gon.GonValueType.String => GonValue.String(oakValue.GetString() ?? string.Empty),
            Oak.Gon.GonValueType.Object => ConvertObject(oakValue),
            Oak.Gon.GonValueType.Array => ConvertArray(oakValue),
            _ => GonValue.Null()
        };
    }

    private static GonValue ConvertObject(Oak.Gon.GonValue oakValue)
    {
        var fields = new Dictionary<string, GonValue>();

        if (oakValue.Fields is not null)
        {
            foreach (var (key, value) in oakValue.Fields)
            {
                fields[key] = ConvertValue(value);
            }
        }

        return GonValue.Object(oakValue.TypeName, oakValue.VariantName, fields);
    }

    private static GonValue ConvertArray(Oak.Gon.GonValue oakValue)
    {
        var elements = new List<GonValue>();

        if (oakValue.Elements is not null)
        {
            foreach (var element in oakValue.Elements)
            {
                elements.Add(ConvertValue(element));
            }
        }

        return GonValue.Array(elements);
    }
}
