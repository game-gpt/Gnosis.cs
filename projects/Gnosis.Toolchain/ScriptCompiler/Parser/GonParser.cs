using Oak.Diagnostics;
using Oak.Gon;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     GON 配置格式解析器（基于 Oak.Gon）
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
        var oakParser = new Oak.Gon.GonParser(_diagnostics);
        return oakParser.Parse(source);
    }
}
