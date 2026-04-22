namespace Gnosis.Core.Platform;

/// <summary>
/// 定义动态库加载器的接口
/// </summary>
public interface INativeLibraryLoader : IDisposable
{
    /// <summary>
    /// 加载指定路径的原生库
    /// </summary>
    /// <param name="path">原生库文件路径</param>
    /// <returns>原生库的句柄</returns>
    nint LoadLibrary(string path);

    /// <summary>
    /// 从已加载的原生库中获取指定符号的地址
    /// </summary>
    /// <param name="library">原生库句柄</param>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号的地址句柄</returns>
    nint GetSymbol(nint library, string symbolName);

    /// <summary>
    /// 释放已加载的原生库
    /// </summary>
    /// <param name="library">原生库句柄</param>
    void FreeLibrary(nint library);
}
