namespace Gnosis.Graphic.RHI;

/// <summary>
///     网格资源接口，封装顶点和索引缓冲区
/// </summary>
public interface IMesh : IDisposable
{
    /// <summary>
    ///     网格名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     顶点数量
    /// </summary>
    uint VertexCount { get; }

    /// <summary>
    ///     索引数量
    /// </summary>
    uint IndexCount { get; }

    /// <summary>
    ///     子网格数量
    /// </summary>
    uint SubMeshCount { get; }
}
