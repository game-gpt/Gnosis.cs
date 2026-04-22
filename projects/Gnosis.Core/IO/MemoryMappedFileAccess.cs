namespace Gnosis.Core.IO;

/// <summary>
/// 内存映射文件访问模式
/// </summary>
public enum MemoryMappedFileAccess
{
    /// <summary>
    /// 只读访问
    /// </summary>
    Read = 0,

    /// <summary>
    /// 只写访问
    /// </summary>
    Write = 1,

    /// <summary>
    /// 读写访问
    /// </summary>
    ReadWrite = 2
}
