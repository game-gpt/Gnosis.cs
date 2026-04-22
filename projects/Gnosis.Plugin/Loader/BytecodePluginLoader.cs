using System;
using System.IO;
using Gnosis.Plugin.Host;
using Gnosis.Plugin.Manifest;
using Gnosis.Runtime.VM;

namespace Gnosis.Plugin.Loader;

/// <summary>
/// gg 字节码插件加载器
/// </summary>
public class BytecodePluginLoader : IPluginLoader
{
    private readonly Func<string, IModule> _moduleFactory;

    /// <summary>
    /// 此加载器支持的插件类型标识
    /// </summary>
    public string SupportedType => "bytecode";

    /// <summary>
    /// 使用模块工厂初始化字节码插件加载器
    /// </summary>
    /// <param name="moduleFactory">用于创建 IModule 实例的工厂方法</param>
    public BytecodePluginLoader(Func<string, IModule> moduleFactory)
    {
        _moduleFactory = moduleFactory;
    }

    /// <summary>
    /// 从指定路径加载字节码插件
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
        var bytecodePath = Path.Combine(manifestDir, manifest.EntryPoint);
        IModule module;

        try
        {
            module = _moduleFactory(bytecodePath);
        }
        catch (FileNotFoundException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"字节码文件不存在：{bytecodePath}", ex);
        }
        catch (IOException ex)
        {
            throw new PluginLoadException(manifest.Id, path, $"读取字节码文件失败：{bytecodePath}", ex);
        }

        if (!module.IsValid)
        {
            throw new PluginLoadException(manifest.Id, path, $"字节码模块无效：{bytecodePath}");
        }

        return new BytecodePlugin(module, manifest);
    }

    /// <summary>
    /// 卸载指定插件
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

    /// <summary>
    /// 字节码插件内部实现，包装 IModule 和 PluginManifest
    /// </summary>
    private sealed class BytecodePlugin : IPlugin
    {
        private readonly IModule _module;

        /// <summary>
        /// 插件清单
        /// </summary>
        public PluginManifest Manifest { get; }

        /// <summary>
        /// 使用模块和清单初始化字节码插件
        /// </summary>
        public BytecodePlugin(IModule module, PluginManifest manifest)
        {
            _module = module;
            Manifest = manifest;
        }

        /// <summary>
        /// 加载时调用（空实现，实际逻辑由 VM 执行）
        /// </summary>
        public void OnLoad(IPluginContext context)
        {
        }

        /// <summary>
        /// 启用时调用（空实现，实际逻辑由 VM 执行）
        /// </summary>
        public void OnEnable()
        {
        }

        /// <summary>
        /// 禁用时调用（空实现，实际逻辑由 VM 执行）
        /// </summary>
        public void OnDisable()
        {
        }

        /// <summary>
        /// 卸载时调用（空实现，实际逻辑由 VM 执行）
        /// </summary>
        public void OnUnload()
        {
        }
    }
}
