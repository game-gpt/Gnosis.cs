namespace Gnosis.Graphic.RHI;

/// <summary>
/// GPU 资源接口
/// </summary>
public interface IResource : IDisposable
{
    /// <summary>
    /// 资源唯一标识
    /// </summary>
    ulong Id { get; }

    /// <summary>
    /// 资源类型
    /// </summary>
    ResourceType ResourceType { get; }

    /// <summary>
    /// 资源格式
    /// </summary>
    ResourceFormat Format { get; }

    /// <summary>
    /// 资源大小（字节）
    /// </summary>
    ulong Size { get; }

    /// <summary>
    /// 是否已释放
    /// </summary>
    bool IsDisposed { get; }
}
