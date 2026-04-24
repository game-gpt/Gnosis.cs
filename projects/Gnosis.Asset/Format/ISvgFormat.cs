using Oak.Svg;

namespace Gnosis.Asset.Format;

/// <summary>
///     SVG 矢量图形格式接口
/// </summary>
public interface ISvgFormat : IFormatHandler
{
    /// <summary>
    ///     加载 SVG 文档
    /// </summary>
    Task<SvgDocument> LoadSvgAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    ///     保存 SVG 文档
    /// </summary>
    Task SaveSvgAsync(string path, SvgDocument document, CancellationToken cancellationToken = default);
}

/// <summary>
///     SVG 矢量图形数据
/// </summary>
public record SvgGraphicData
{
    /// <summary>
    ///     名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     文档宽度
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     文档高度
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     视图框
    /// </summary>
    public float[] ViewBox { get; init; } = [];

    /// <summary>
    ///     SVG 文档原始内容
    /// </summary>
    public string SourceContent { get; init; } = string.Empty;
}
