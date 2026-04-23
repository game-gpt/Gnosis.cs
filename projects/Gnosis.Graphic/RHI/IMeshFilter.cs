namespace Gnosis.Graphic.RHI;

/// <summary>
///     网格过滤器接口，引用网格资源
/// </summary>
public interface IMeshFilter
{
    /// <summary>
    ///     关联的网格资源
    /// </summary>
    IMesh Mesh { get; }

    /// <summary>
    ///     子网格索引
    /// </summary>
    uint SubMeshIndex { get; }
}
