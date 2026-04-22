using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件清单格式验证器
/// </summary>
public static class PluginManifestValidator
{
    /// <summary>
    /// 插件标识格式正则表达式，反向域名格式
    /// </summary>
    private static readonly Regex PluginIdPattern = new(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$", RegexOptions.Compiled);

    /// <summary>
    /// 验证插件清单格式
    /// </summary>
    /// <param name="manifest">待验证的插件清单</param>
    /// <returns>验证结果</returns>
    public static ValidationResult Validate(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors = new List<string>();

        ValidatePluginId(manifest.Id, errors);
        ValidateVersion(manifest.Version, errors);
        ValidateDependencies(manifest.Dependencies, errors);
        ValidateEntryPoint(manifest.EntryPoint, errors);
        ValidateApiVersion(manifest.ApiVersion, errors);

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证插件标识格式
    /// </summary>
    private static void ValidatePluginId(string id, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add("插件标识不能为空");
            return;
        }

        if (!PluginIdPattern.IsMatch(id))
        {
            errors.Add($"插件标识格式无效，应为反向域名格式（如 com.gnosis.example）：{id}");
        }
    }

    /// <summary>
    /// 验证版本号格式
    /// </summary>
    private static void ValidateVersion(PluginVersion version, List<string> errors)
    {
        if (version is null)
        {
            errors.Add("版本号不能为空");
            return;
        }

        if (version.Major < 0)
        {
            errors.Add($"主版本号不能为负数：{version.Major}");
        }

        if (version.Minor < 0)
        {
            errors.Add($"次版本号不能为负数：{version.Minor}");
        }

        if (version.Patch < 0)
        {
            errors.Add($"修订号不能为负数：{version.Patch}");
        }
    }

    /// <summary>
    /// 验证依赖列表有效性
    /// </summary>
    private static void ValidateDependencies(IReadOnlyList<PluginDependency> dependencies, List<string> errors)
    {
        if (dependencies is null)
        {
            return;
        }

        for (var i = 0; i < dependencies.Count; i++)
        {
            var dep = dependencies[i];

            if (string.IsNullOrWhiteSpace(dep.PluginId))
            {
                errors.Add($"第 {i + 1} 个依赖的插件标识不能为空");
            }

            if (string.IsNullOrWhiteSpace(dep.VersionRange))
            {
                errors.Add($"第 {i + 1} 个依赖的版本范围不能为空");
            }
        }
    }

    /// <summary>
    /// 验证入口点非空
    /// </summary>
    private static void ValidateEntryPoint(string entryPoint, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            errors.Add("入口点不能为空");
        }
    }

    /// <summary>
    /// 验证 API 版本非空
    /// </summary>
    private static void ValidateApiVersion(string apiVersion, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            errors.Add("API 版本不能为空");
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public sealed record ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// 初始化验证结果
        /// </summary>
        public ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }
    }
}
