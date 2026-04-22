using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gnosis.Plugin.Host;
using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Loader;

/// <summary>
/// 插件加载器注册表
/// </summary>
public class PluginLoaderRegistry
{
    #region Fields

    private readonly Dictionary<string, IPluginLoader> _loaders = new();

    #endregion

    #region Properties

    /// <summary>
    /// 已注册的加载器类型列表
    /// </summary>
    public IReadOnlyList<string> RegisteredTypes => _loaders.Keys.ToList();

    #endregion

    #region Public Methods

    /// <summary>
    /// 注册加载器
    /// </summary>
    /// <param name="loader">要注册的加载器</param>
    public void RegisterLoader(IPluginLoader loader)
    {
        _loaders[loader.SupportedType] = loader;
    }

    /// <summary>
    /// 移除加载器
    /// </summary>
    /// <param name="supportedType">要移除的加载器类型标识</param>
    public void UnregisterLoader(string supportedType)
    {
        _loaders.Remove(supportedType);
    }

    /// <summary>
    /// 按类型获取加载器
    /// </summary>
    /// <param name="supportedType">加载器类型标识</param>
    /// <returns>对应的加载器，未找到返回 null</returns>
    public IPluginLoader? GetLoader(string supportedType)
    {
        return _loaders.GetValueOrDefault(supportedType);
    }

    /// <summary>
    /// 使用指定类型的加载器加载插件
    /// </summary>
    /// <param name="path">插件路径</param>
    /// <param name="pluginType">插件类型标识</param>
    /// <returns>已加载的插件实例</returns>
    public IPlugin LoadPlugin(string path, string pluginType)
    {
        var loader = GetLoader(pluginType);

        if (loader is null)
        {
            throw new PluginLoadException(null, path, $"未注册的插件类型加载器：{pluginType}");
        }

        return loader.Load(path);
    }

    /// <summary>
    /// 自动检测插件类型并加载
    /// </summary>
    /// <param name="path">插件路径</param>
    /// <returns>已加载的插件实例</returns>
    public IPlugin LoadPluginAuto(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new PluginLoadException(null, path, "插件路径不能为空");
        }

        var manifestFilePath = ResolveManifestPath(path);
        PluginManifest manifest;

        try
        {
            manifest = PluginManifestParser.ParseFromFile(manifestFilePath);
        }
        catch (PluginManifestException ex)
        {
            throw new PluginLoadException(null, path, $"自动检测插件类型时解析清单失败：{manifestFilePath}", ex);
        }

        var extension = Path.GetExtension(manifest.EntryPoint).ToLowerInvariant();
        var pluginType = extension switch
        {
            ".dll" => "assembly",
            ".ggbc" => "bytecode",
            _ => null
        };

        if (pluginType is null)
        {
            throw new PluginLoadException(manifest.Id, path, $"无法识别的入口点扩展名：{extension}");
        }

        var loader = GetLoader(pluginType);

        if (loader is null)
        {
            throw new PluginLoadException(manifest.Id, path, $"未注册的插件类型加载器：{pluginType}");
        }

        return loader.Load(path);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 解析清单文件路径
    /// </summary>
    private static string ResolveManifestPath(string path)
    {
        if (Directory.Exists(path))
        {
            return Path.Combine(path, "plugin.json");
        }

        return path;
    }

    #endregion
}
