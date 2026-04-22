using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gnosis.Plugin.Host;
using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Loader;

/// <summary>
/// C# 程序集插件加载器
/// </summary>
public class AssemblyPluginLoader : IPluginLoader
{
    /// <summary>
    /// 此加载器支持的插件类型标识
    /// </summary>
    public string SupportedType => "assembly";

    /// <summary>
    /// 从指定路径加载程序集插件
    /// </summary>
    /// <param name="path">插件路径（目录或清单文件路径）</param>
    /// <returns>已加载的插件实例</returns>
    public IPlugin Load(string path)
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
            throw new PluginLoadException(null, path, $"解析插件清单失败：{manifestFilePath}", ex);
        }

        var manifestDir = Path.GetDirectoryName(manifestFilePath)!;
        var assemblyPath = Path.Combine(manifestDir, manifest.EntryPoint);
        Assembly assembly;

        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (FileNotFoundException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"程序集文件不存在：{assemblyPath}", ex);
        }
        catch (FileLoadException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"程序集加载失败：{assemblyPath}", ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"程序集格式无效：{assemblyPath}", ex);
        }

        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        if (pluginType is null)
        {
            throw new PluginLoadException(manifest.Id, path, $"程序集中未找到实现 IPlugin 接口的类型：{assemblyPath}");
        }

        object? instance;

        try
        {
            instance = Activator.CreateInstance(pluginType);
        }
        catch (MissingMethodException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"无法创建插件实例，缺少无参构造函数：{pluginType.FullName}", ex);
        }
        catch (TargetInvocationException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"插件构造函数执行失败：{pluginType.FullName}", ex);
        }

        if (instance is not IPlugin plugin)
        {
            throw new PluginLoadException(manifest.Id, path, $"创建的实例不是有效的 IPlugin 实现：{pluginType.FullName}");
        }

        return plugin;
    }

    /// <summary>
    /// 卸载指定插件，仅调用生命周期钩子
    /// </summary>
    /// <param name="plugin">要卸载的插件</param>
    public void Unload(IPlugin plugin)
    {
        plugin.OnUnload();
    }

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
}
