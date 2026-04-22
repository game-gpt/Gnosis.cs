using System;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Platform;

/// <summary>
/// 跨平台原生库加载器的实现
/// </summary>
public class NativeLibraryLoader : INativeLibraryLoader
{
    /// <summary>
    /// 加载指定路径的原生库
    /// </summary>
    /// <param name="path">原生库文件路径</param>
    /// <returns>原生库的句柄</returns>
    public nint LoadLibrary(string path)
    {
        var handle = NativeLibrary.Load(path);

        return handle;
    }

    /// <summary>
    /// 从已加载的原生库中获取指定符号的地址
    /// </summary>
    /// <param name="library">原生库句柄</param>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号的地址句柄，未找到则返回 <see cref="nint.Zero"/></returns>
    public nint GetSymbol(nint library, string symbolName)
    {
        if (NativeLibrary.TryGetExport(library, symbolName, out var symbol))
        {
            return symbol;
        }

        return nint.Zero;
    }

    /// <summary>
    /// 释放已加载的原生库
    /// </summary>
    /// <param name="library">原生库句柄</param>
    public void FreeLibrary(nint library)
    {
        if (library != nint.Zero)
        {
            NativeLibrary.Free(library);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的核心方法
    /// </summary>
    /// <param name="disposing">是否为显式释放</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}
