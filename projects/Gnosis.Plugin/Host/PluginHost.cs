using Gnosis.Plugin.Context;
using Gnosis.Plugin.Dependency;
using Gnosis.Plugin.Extension;
using Gnosis.Plugin.Isolation;
using Gnosis.Plugin.Loader;
using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件宿主实现（含生命周期状态机、热插拔、依赖解析、沙箱隔离、扩展点与服务发现）
/// </summary>
public class PluginHost : IPluginHost
{
    #region 字段

    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly Dictionary<string, PluginContext> _contexts = new();
    private readonly Dictionary<string, PluginIsolationContext> _isolationContexts = new();
    private readonly PluginLoaderRegistry _loaderRegistry;
    private readonly DependencyResolver _dependencyResolver;
    private readonly PermissionChecker _permissionChecker;
    private readonly SandboxPolicy _defaultSandboxPolicy;
    private readonly ExtensionRegistry _extensionRegistry;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly SignatureValidator _signatureValidator;

    #endregion

    #region 属性

    /// <summary>
    /// 已加载插件
    /// </summary>
    public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => _plugins;

    /// <summary>
    /// 插件加载器注册表
    /// </summary>
    public PluginLoaderRegistry LoaderRegistry => _loaderRegistry;

    /// <summary>
    /// 依赖解析器
    /// </summary>
    public DependencyResolver DependencyResolver => _dependencyResolver;

    /// <summary>
    /// 权限检查器
    /// </summary>
    public PermissionChecker PermissionChecker => _permissionChecker;

    /// <summary>
    /// 扩展点注册表
    /// </summary>
    public ExtensionRegistry ExtensionRegistry => _extensionRegistry;

    /// <summary>
    /// 服务注册表
    /// </summary>
    public ServiceRegistry ServiceRegistry => _serviceRegistry;

    /// <summary>
    /// 签名校验器
    /// </summary>
    public SignatureValidator SignatureValidator => _signatureValidator;

    #endregion

    #region 事件

    /// <summary>
    /// 插件加载事件
    /// </summary>
    public event EventHandler<PluginEventArgs>? PluginLoaded;

    /// <summary>
    /// 插件卸载事件
    /// </summary>
    public event EventHandler<PluginEventArgs>? PluginUnloaded;

    /// <summary>
    /// 插件启用事件
    /// </summary>
    public event EventHandler<PluginEventArgs>? PluginEnabled;

    /// <summary>
    /// 插件禁用事件
    /// </summary>
    public event EventHandler<PluginEventArgs>? PluginDisabled;

    /// <summary>
    /// 插件热重载事件
    /// </summary>
    public event EventHandler<PluginHotReloadEventArgs>? PluginHotReloaded;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化插件宿主
    /// </summary>
    /// <param name="loaderRegistry">插件加载器注册表，为 null 时创建空注册表</param>
    /// <param name="permissionPolicy">权限策略，为 null 时使用默认策略</param>
    /// <param name="sandboxPolicy">沙箱策略，为 null 时使用默认策略</param>
    /// <param name="signatureValidationMode">签名验证模式，默认为 Optional</param>
    public PluginHost(
        PluginLoaderRegistry? loaderRegistry = null,
        PermissionPolicy? permissionPolicy = null,
        SandboxPolicy? sandboxPolicy = null,
        SignatureValidationMode signatureValidationMode = SignatureValidationMode.Optional)
    {
        _loaderRegistry = loaderRegistry ?? new PluginLoaderRegistry();
        _dependencyResolver = new DependencyResolver();
        _permissionChecker = new PermissionChecker(permissionPolicy);
        _defaultSandboxPolicy = sandboxPolicy ?? SandboxPolicy.Default;
        _extensionRegistry = new ExtensionRegistry();
        _serviceRegistry = new ServiceRegistry();
        _signatureValidator = new SignatureValidator(signatureValidationMode);
    }

    #endregion

    #region IPluginHost 实现

    /// <summary>
    /// 加载插件
    /// </summary>
    /// <param name="path">插件路径</param>
    /// <returns>已加载的插件实例</returns>
    public IPlugin LoadPlugin(string path)
    {
        var plugin = _loaderRegistry.LoadPluginAuto(path);
        var pluginId = plugin.Manifest.Id;

        if (_plugins.ContainsKey(pluginId))
        {
            throw new PluginLifecycleException(pluginId, PluginState.Loaded, PluginState.Loaded);
        }

        var validationResult = PluginManifestValidator.Validate(plugin.Manifest);

        if (!validationResult.IsValid)
        {
            throw new PluginManifestException(pluginId, $"插件清单验证失败：{string.Join("；", validationResult.Errors)}");
        }

        var signatureResult = _signatureValidator.ValidateManifest(plugin.Manifest);

        if (!signatureResult.IsValid)
        {
            throw new PluginManifestException(pluginId, $"插件签名验证失败：{signatureResult.ErrorMessage}");
        }

        var context = new PluginContext(pluginId, plugin.Manifest, this);
        var isolationContext = new PluginIsolationContext(plugin.Manifest, _defaultSandboxPolicy);

        _plugins[pluginId] = plugin;
        _contexts[pluginId] = context;
        _isolationContexts[pluginId] = isolationContext;

        _dependencyResolver.Register(plugin.Manifest);

        plugin.OnLoad(context);

        OnPluginLoaded(new PluginEventArgs(pluginId, plugin));

        return plugin;
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    public void UnloadPlugin(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var plugin))
        {
            return;
        }

        var context = _contexts[pluginId];

        if (context.State != PluginState.Disabled)
        {
            throw new PluginLifecycleException(pluginId, context.State, PluginState.Unloaded);
        }

        var dependents = _dependencyResolver.GetDependents(pluginId);

        if (dependents.Count > 0)
        {
            throw new PluginLifecycleException(
                pluginId,
                context.State,
                PluginState.Unloaded
            );
        }

        context.SetState(PluginState.Unloading);
        plugin.OnUnload();

        _dependencyResolver.Unregister(pluginId);
        _extensionRegistry.UnregisterExtensionsByPlugin(pluginId);
        _extensionRegistry.UnregisterExtensionPointsByOwner(pluginId);
        _serviceRegistry.UnregisterByPlugin(pluginId);

        if (_isolationContexts.TryGetValue(pluginId, out var isolationCtx))
        {
            isolationCtx.Dispose();
            _isolationContexts.Remove(pluginId);
        }

        _plugins.Remove(pluginId);
        _contexts.Remove(pluginId);

        OnPluginUnloaded(new PluginEventArgs(pluginId, plugin));
    }

    /// <summary>
    /// 启用插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    public void EnablePlugin(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var plugin))
        {
            return;
        }

        var context = _contexts[pluginId];

        if (context.State != PluginState.Loaded && context.State != PluginState.Disabled)
        {
            throw new PluginLifecycleException(pluginId, context.State, PluginState.Enabled);
        }

        if (!_dependencyResolver.AreDependenciesSatisfied(pluginId))
        {
            throw new PluginLifecycleException(pluginId, context.State, PluginState.Enabled);
        }

        plugin.OnEnable();
        context.SetState(PluginState.Enabled);

        if (_isolationContexts.TryGetValue(pluginId, out var isolationCtx))
        {
            isolationCtx.Resume();
        }

        OnPluginEnabled(new PluginEventArgs(pluginId, plugin));
    }

    /// <summary>
    /// 禁用插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    public void DisablePlugin(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var plugin))
        {
            return;
        }

        var context = _contexts[pluginId];

        if (context.State != PluginState.Enabled)
        {
            throw new PluginLifecycleException(pluginId, context.State, PluginState.Disabled);
        }

        plugin.OnDisable();
        context.SetState(PluginState.Disabled);

        if (_isolationContexts.TryGetValue(pluginId, out var isolationCtx))
        {
            isolationCtx.Suspend();
        }

        OnPluginDisabled(new PluginEventArgs(pluginId, plugin));
    }

    /// <summary>
    /// 按ID查询插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <returns>插件实例，未找到返回 null</returns>
    public IPlugin? GetPlugin(string pluginId)
    {
        return _plugins.GetValueOrDefault(pluginId);
    }

    /// <summary>
    /// 按ID查询上下文
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <returns>插件上下文，未找到返回 null</returns>
    public IPluginContext? GetPluginContext(string pluginId)
    {
        return _contexts.GetValueOrDefault(pluginId);
    }

    #endregion

    #region 热插拔

    /// <summary>
    /// 热重载插件（运行时替换插件实现）
    /// </summary>
    /// <param name="pluginId">要热重载的插件标识</param>
    /// <param name="newPluginPath">新插件路径</param>
    /// <returns>热重载后的插件实例</returns>
    /// <exception cref="PluginLifecycleException">插件未加载或不支持热重载时抛出</exception>
    public IPlugin HotReloadPlugin(string pluginId, string newPluginPath)
    {
        if (!_plugins.TryGetValue(pluginId, out var oldPlugin))
        {
            throw new PluginLifecycleException(pluginId, PluginState.Unloaded, PluginState.Loaded);
        }

        if (!_contexts.TryGetValue(pluginId, out var context))
        {
            throw new PluginLifecycleException(pluginId, PluginState.Unloaded, PluginState.Loaded);
        }

        var wasEnabled = context.State == PluginState.Enabled;

        if (wasEnabled)
        {
            oldPlugin.OnDisable();
        }

        OnPluginHotReloading(new PluginHotReloadEventArgs(pluginId, oldPlugin, null, false, null));

        var oldManifest = oldPlugin.Manifest;

        _dependencyResolver.Unregister(pluginId);
        _extensionRegistry.UnregisterExtensionsByPlugin(pluginId);
        _serviceRegistry.UnregisterByPlugin(pluginId);

        var newPlugin = _loaderRegistry.LoadPluginAuto(newPluginPath);

        if (newPlugin.Manifest.Id != pluginId)
        {
            _dependencyResolver.Register(oldManifest);

            if (wasEnabled)
            {
                oldPlugin.OnEnable();
            }

            throw new PluginLifecycleException(
                pluginId,
                context.State,
                PluginState.Loaded
            );
        }

        var validationResult = PluginManifestValidator.Validate(newPlugin.Manifest);

        if (!validationResult.IsValid)
        {
            _dependencyResolver.Register(oldManifest);

            if (wasEnabled)
            {
                oldPlugin.OnEnable();
            }

            throw new PluginManifestException(
                pluginId,
                $"新插件清单验证失败：{string.Join("；", validationResult.Errors)}"
            );
        }

        var signatureResult = _signatureValidator.ValidateManifest(newPlugin.Manifest);

        if (!signatureResult.IsValid)
        {
            _dependencyResolver.Register(oldManifest);

            if (wasEnabled)
            {
                oldPlugin.OnEnable();
            }

            throw new PluginManifestException(
                pluginId,
                $"新插件签名验证失败：{signatureResult.ErrorMessage}"
            );
        }

        _plugins[pluginId] = newPlugin;
        _dependencyResolver.Register(newPlugin.Manifest);

        var newContext = new PluginContext(pluginId, newPlugin.Manifest, this);

        if (wasEnabled)
        {
            newContext.SetState(PluginState.Disabled);
        }

        _contexts[pluginId] = newContext;

        newPlugin.OnLoad(newContext);

        if (wasEnabled)
        {
            newPlugin.OnEnable();
            newContext.SetState(PluginState.Enabled);
        }

        OnPluginHotReloaded(new PluginHotReloadEventArgs(pluginId, oldPlugin, newPlugin, true, null));

        return newPlugin;
    }

    /// <summary>
    /// 检查插件是否支持热重载
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>是否支持热重载</returns>
    public bool CanHotReload(string pluginId)
    {
        if (!_plugins.ContainsKey(pluginId))
        {
            return false;
        }

        var dependents = _dependencyResolver.GetDependents(pluginId);

        return dependents.Count == 0;
    }

    #endregion

    #region 隔离上下文访问

    /// <summary>
    /// 获取插件的隔离上下文
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>隔离上下文，未找到返回 null</returns>
    public PluginIsolationContext? GetIsolationContext(string pluginId)
    {
        return _isolationContexts.GetValueOrDefault(pluginId);
    }

    #endregion

    #region 依赖解析

    /// <summary>
    /// 解析所有已注册插件的依赖关系
    /// </summary>
    /// <returns>依赖解析结果</returns>
    public DependencyResolveResult ResolveDependencies()
    {
        return _dependencyResolver.Resolve();
    }

    #endregion

    #region 事件触发

    /// <summary>
    /// 触发插件加载事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginLoaded(PluginEventArgs e)
    {
        PluginLoaded?.Invoke(this, e);
    }

    /// <summary>
    /// 触发插件卸载事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginUnloaded(PluginEventArgs e)
    {
        PluginUnloaded?.Invoke(this, e);
    }

    /// <summary>
    /// 触发插件启用事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginEnabled(PluginEventArgs e)
    {
        PluginEnabled?.Invoke(this, e);
    }

    /// <summary>
    /// 触发插件禁用事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginDisabled(PluginEventArgs e)
    {
        PluginDisabled?.Invoke(this, e);
    }

    /// <summary>
    /// 触发插件热重载中事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginHotReloading(PluginHotReloadEventArgs e)
    {
    }

    /// <summary>
    /// 触发插件热重载完成事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnPluginHotReloaded(PluginHotReloadEventArgs e)
    {
        PluginHotReloaded?.Invoke(this, e);
    }

    #endregion
}
