using System.Text;
using System.Text.Json;
using Oak.Svg;

namespace Gnosis.Asset.Format;

/// <summary>
///     SVG 矢量图形格式处理器，支持 SVG 文件加载和保存
/// </summary>
public class SvgFormatHandler : FormatHandlerBase, ISvgFormat
{
    #region 常量

    private const string EngineExtension = ".gnosis-svg";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    #endregion

    #region 属性

    public override FormatType SupportedFormat => FormatType.VectorGraphic;

    #endregion

    #region 构造函数

    public SvgFormatHandler() { }

    public SvgFormatHandler(IFileIO fileIO) : base(fileIO) { }

    #endregion

    #region 加载 SVG

    /// <summary>
    ///     加载 SVG 文档，支持引擎格式和标准 SVG 格式
    /// </summary>
    public async Task<SvgDocument> LoadSvgAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到 SVG 文件：{path}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == EngineExtension)
        {
            return await LoadEngineFormatAsync(path, cancellationToken);
        }

        return await LoadStandardFormatAsync(path, cancellationToken);
    }

    /// <summary>
    ///     加载引擎格式 SVG（JSON 序列化的 SvgGraphicData）
    /// </summary>
    private async Task<SvgDocument> LoadEngineFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var sourceContent = Encoding.UTF8.GetString(data);
        var parser = new SvgParser();
        return parser.Parse(sourceContent);
    }

    /// <summary>
    ///     加载标准 SVG 格式
    /// </summary>
    private async Task<SvgDocument> LoadStandardFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var content = Encoding.UTF8.GetString(data);
        var parser = new SvgParser();
        return await Task.Run(() => parser.Parse(content), cancellationToken);
    }

    #endregion

    #region 保存 SVG

    /// <summary>
    ///     保存 SVG 文档，支持引擎格式和标准 SVG 格式
    /// </summary>
    public async Task SaveSvgAsync(string path, SvgDocument document, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == EngineExtension)
        {
            await SaveEngineFormatAsync(path, document, cancellationToken);
            return;
        }

        await SaveStandardFormatAsync(path, document, cancellationToken);
    }

    /// <summary>
    ///     保存为引擎格式
    /// </summary>
    private async Task SaveEngineFormatAsync(string path, SvgDocument document, CancellationToken cancellationToken)
    {
        var graphicData = new SvgGraphicData
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Width = document.Width,
            Height = document.Height,
            ViewBox = document.ViewBox
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(graphicData, JsonOptions);
        await WriteAsync(path, json, null, cancellationToken);
    }

    /// <summary>
    ///     保存为标准 SVG 格式（原始内容回写）
    /// </summary>
    private async Task SaveStandardFormatAsync(string path, SvgDocument document, CancellationToken cancellationToken)
    {
        var content = SerializeSvgDocument(document);
        var data = Encoding.UTF8.GetBytes(content);
        await WriteAsync(path, data, null, cancellationToken);
    }

    #endregion

    #region 验证

    /// <summary>
    ///     验证 SVG 文件格式
    /// </summary>
    public override async Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            return false;
        }

        try
        {
            var data = await ReadAsync(path, cancellationToken);

            if (data.Length < 5)
            {
                return false;
            }

            var header = Encoding.UTF8.GetString(data[..Math.Min(256, data.Length)]);

            return header.Contains("<svg") || header.Contains("<?xml");
        }
        catch (IOException)
        {
            return false;
        }
    }

    #endregion

    #region 辅助方法

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string>
        {
            EngineExtension, ".svg", ".svgz"
        };
    }

    /// <summary>
    ///     将 SvgDocument 序列化为 SVG XML 文本
    /// </summary>
    private static string SerializeSvgDocument(SvgDocument document)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");

        var root = document.Root;
        var viewBoxAttr = root.ViewBox.Length >= 4
            ? $" viewBox=\"{root.ViewBox[0]} {root.ViewBox[1]} {root.ViewBox[2]} {root.ViewBox[3]}\""
            : "";

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{root.Width}\" height=\"{root.Height}\"{viewBoxAttr}>\n");
        SerializeChildren(root.Children, sb, 1);
        sb.Append("</svg>\n");

        return sb.ToString();
    }

    private static void SerializeChildren(List<SvgElement> children, StringBuilder sb, int indent)
    {
        var prefix = new string(' ', indent * 2);

        foreach (var child in children)
        {
            switch (child)
            {
                case SvgPathElement path:
                    var d = string.Join(" ", path.Commands.Select(SerializePathCommand));
                    sb.Append($"{prefix}<path d=\"{d}\"");
                    AppendCommonAttributes(child, sb);
                    sb.Append("/>\n");
                    break;

                case SvgRectElement rect:
                    sb.Append($"{prefix}<rect x=\"{rect.X}\" y=\"{rect.Y}\" width=\"{rect.Width}\" height=\"{rect.Height}\"");
                    if (rect.HasRoundedCorners) sb.Append($" rx=\"{rect.Rx}\" ry=\"{rect.Ry}\"");
                    AppendCommonAttributes(child, sb);
                    sb.Append("/>\n");
                    break;

                case SvgCircleElement circle:
                    sb.Append($"{prefix}<circle cx=\"{circle.Cx}\" cy=\"{circle.Cy}\" r=\"{circle.R}\"");
                    AppendCommonAttributes(child, sb);
                    sb.Append("/>\n");
                    break;

                case SvgEllipseElement ellipse:
                    sb.Append($"{prefix}<ellipse cx=\"{ellipse.Cx}\" cy=\"{ellipse.Cy}\" rx=\"{ellipse.Rx}\" ry=\"{ellipse.Ry}\"");
                    AppendCommonAttributes(child, sb);
                    sb.Append("/>\n");
                    break;

                case SvgLineElement line:
                    sb.Append($"{prefix}<line x1=\"{line.X1}\" y1=\"{line.Y1}\" x2=\"{line.X2}\" y2=\"{line.Y2}\"");
                    AppendCommonAttributes(child, sb);
                    sb.Append("/>\n");
                    break;

                case SvgGroupElement:
                    sb.Append($"{prefix}<g");
                    AppendCommonAttributes(child, sb);
                    sb.Append(">\n");
                    SerializeChildren(child.Children, sb, indent + 1);
                    sb.Append($"{prefix}</g>\n");
                    break;

                default:
                    if (child.IsContainer)
                    {
                        sb.Append($"{prefix}<!-- {child.ElementType} -->\n");
                        SerializeChildren(child.Children, sb, indent + 1);
                    }

                    break;
            }
        }
    }

    private static void AppendCommonAttributes(SvgElement element, StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(element.Id))
        {
            sb.Append($" id=\"{element.Id}\"");
        }

        if (element.Transforms.Count > 0)
        {
            var transforms = string.Join(" ", element.Transforms.Select(SerializeTransform));
            sb.Append($" transform=\"{transforms}\"");
        }

        if (element.Style is { Fill: not null, IsFillNone: false })
        {
            sb.Append($" fill=\"{element.Style.Fill}\"");
        }
        else if (element.Style.IsFillNone)
        {
            sb.Append(" fill=\"none\"");
        }

        if (element.Style is { Stroke: not null, IsStrokeNone: false })
        {
            sb.Append($" stroke=\"{element.Style.Stroke}\"");
            if (element.Style.StrokeWidth > 0)
            {
                sb.Append($" stroke-width=\"{element.Style.StrokeWidth}\"");
            }
        }
    }

    private static string SerializePathCommand(SvgPathCommand cmd)
    {
        var prefix = cmd.Type switch
        {
            SvgPathCommandType.MoveTo => "M",
            SvgPathCommandType.RelativeMoveTo => "m",
            SvgPathCommandType.LineTo => "L",
            SvgPathCommandType.RelativeLineTo => "l",
            SvgPathCommandType.HorizontalLineTo => "H",
            SvgPathCommandType.RelativeHorizontalLineTo => "h",
            SvgPathCommandType.VerticalLineTo => "V",
            SvgPathCommandType.RelativeVerticalLineTo => "v",
            SvgPathCommandType.CurveTo => "C",
            SvgPathCommandType.RelativeCurveTo => "c",
            SvgPathCommandType.SmoothCurveTo => "S",
            SvgPathCommandType.RelativeSmoothCurveTo => "s",
            SvgPathCommandType.QuadraticCurveTo => "Q",
            SvgPathCommandType.RelativeQuadraticCurveTo => "q",
            SvgPathCommandType.SmoothQuadraticCurveTo => "T",
            SvgPathCommandType.RelativeSmoothQuadraticCurveTo => "t",
            SvgPathCommandType.ArcTo => "A",
            SvgPathCommandType.RelativeArcTo => "a",
            SvgPathCommandType.ClosePath => "Z",
            _ => ""
        };

        if (cmd.Arguments.Length == 0)
        {
            return prefix;
        }

        return $"{prefix} {string.Join(" ", cmd.Arguments.Select(a => FormatFloat(a)))}";
    }

    private static string SerializeTransform(SvgTransform transform)
    {
        return transform.Type switch
        {
            SvgTransformType.Matrix => $"matrix({string.Join(",", transform.Arguments.Select(FormatFloat))})",
            SvgTransformType.Translate => transform.Arguments.Length >= 2
                ? $"translate({FormatFloat(transform.Arguments[0])},{FormatFloat(transform.Arguments[1])})"
                : $"translate({FormatFloat(transform.Arguments[0])})",
            SvgTransformType.Scale => transform.Arguments.Length >= 2
                ? $"scale({FormatFloat(transform.Arguments[0])},{FormatFloat(transform.Arguments[1])})"
                : $"scale({FormatFloat(transform.Arguments[0])})",
            SvgTransformType.Rotate => transform.Arguments.Length >= 3
                ? $"rotate({FormatFloat(transform.Arguments[0])},{FormatFloat(transform.Arguments[1])},{FormatFloat(transform.Arguments[2])})"
                : $"rotate({FormatFloat(transform.Arguments[0])})",
            SvgTransformType.SkewX => $"skewX({FormatFloat(transform.Arguments[0])})",
            SvgTransformType.SkewY => $"skewY({FormatFloat(transform.Arguments[0])})",
            _ => ""
        };
    }

    private static string FormatFloat(float value)
    {
        if (value == MathF.Truncate(value))
        {
            return ((int)value).ToString();
        }

        return value.ToString("G");
    }

    #endregion
}
