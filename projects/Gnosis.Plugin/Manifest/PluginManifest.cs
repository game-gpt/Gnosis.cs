using System;
using System.Collections.Generic;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件清单主数据模型
/// </summary>
public sealed record PluginManifest
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 插件版本
    /// </summary>
    public PluginVersion Version { get; }

    /// <summary>
    /// 插件描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 插件作者
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// 插件入口点
    /// </summary>
    public string EntryPoint { get; }

    /// <summary>
    /// 插件依赖列表
    /// </summary>
    public IReadOnlyList<PluginDependency> Dependencies { get; }

    /// <summary>
    /// 插件所需权限
    /// </summary>
    public PluginPermission Permissions { get; }

    /// <summary>
    /// 插件签名
    /// </summary>
    public string? Signature { get; }

    /// <summary>
    /// 目标 API 版本
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    /// 显示名称，格式为 "名称 v版本号"
    /// </summary>
    public string DisplayName => $"{Name} v{Version}";

    /// <summary>
    /// 初始化插件清单
    /// </summary>
    public PluginManifest(
        string id,
        string name,
        PluginVersion version,
        string description,
        string author,
        string entryPoint,
        IReadOnlyList<PluginDependency>? dependencies = null,
        PluginPermission permissions = PluginPermission.None,
        string? signature = null,
        string apiVersion = "")
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
        Author = author;
        EntryPoint = entryPoint;
        Dependencies = dependencies ?? Array.Empty<PluginDependency>();
        Permissions = permissions;
        Signature = signature;
        ApiVersion = apiVersion;
    }
}
