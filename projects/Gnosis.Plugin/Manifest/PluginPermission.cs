using System;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件权限声明标志
/// </summary>
[Flags]
public enum PluginPermission
{
    /// <summary>
    /// 无权限
    /// </summary>
    None = 0,

    /// <summary>
    /// 文件系统读取权限
    /// </summary>
    FileSystemRead = 1 << 0,

    /// <summary>
    /// 文件系统写入权限
    /// </summary>
    FileSystemWrite = 1 << 1,

    /// <summary>
    /// 网络访问权限
    /// </summary>
    NetworkAccess = 1 << 2,

    /// <summary>
    /// 原生调用权限
    /// </summary>
    NativeCall = 1 << 3,

    /// <summary>
    /// 进程创建权限
    /// </summary>
    ProcessSpawn = 1 << 4,

    /// <summary>
    /// 内存管理权限
    /// </summary>
    MemoryManagement = 1 << 5,

    /// <summary>
    /// 线程操作权限
    /// </summary>
    Threading = 1 << 6,

    /// <summary>
    /// 所有权限
    /// </summary>
    All = ~0
}
