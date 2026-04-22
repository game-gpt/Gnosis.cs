using Gnosis.Plugin.Host;

namespace Gnosis.Plugin.Loader;

/// <summary>
/// 插件加载器接口
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// 此加载器支持的插件类型标识
    /// </summary>
    string SupportedType { get; }

    /// <summary>
    /// 从指定路径加载插件
    /// </summary>
    /// <param name="path">插件路径</param>
    /// <returns>已加载的插件实例</returns>
    IPlugin Load(string path);

    /// <summary>
    /// 卸载指定插件
    /// </summary>
    /// <param name="plugin">要卸载的插件</param>
    void Unload(IPlugin plugin);
}
