using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// JSON 插件清单解析器
/// </summary>
public static class PluginManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 从 JSON 字符串解析插件清单
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>解析后的插件清单</returns>
    /// <exception cref="PluginManifestException">JSON 格式无效或必填字段缺失时抛出</exception>
    public static PluginManifest Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new PluginManifestException("清单 JSON 内容不能为空");
        }

        JsonDocument doc;

        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new PluginManifestException("清单 JSON 格式无效", ex);
        }

        var root = doc.RootElement;

        var id = GetRequiredString(root, "id");
        var name = GetRequiredString(root, "name");
        var versionStr = GetRequiredString(root, "version");
        var entryPoint = GetRequiredString(root, "entryPoint");
        var apiVersion = GetRequiredString(root, "apiVersion");

        if (!PluginVersion.TryParse(versionStr, out var version))
        {
            throw new PluginManifestException(id, $"版本号格式无效：{versionStr}");
        }

        var description = GetStringOrDefault(root, "description", "");
        var author = GetStringOrDefault(root, "author", "");
        var signature = GetStringOrNull(root, "signature");
        var dependencies = ParseDependencies(root);
        var permissions = ParsePermissions(root);

        return new PluginManifest(
            id,
            name,
            version!,
            description,
            author,
            entryPoint,
            dependencies,
            permissions,
            signature,
            apiVersion
        );
    }

    /// <summary>
    /// 从文件解析插件清单
    /// </summary>
    /// <param name="filePath">清单文件路径</param>
    /// <returns>解析后的插件清单</returns>
    /// <exception cref="PluginManifestException">文件读取失败或解析失败时抛出</exception>
    public static PluginManifest ParseFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new PluginManifestException("文件路径不能为空");
        }

        string json;

        try
        {
            json = File.ReadAllText(filePath);
        }
        catch (FileNotFoundException)
        {
            throw new PluginManifestException($"清单文件不存在：{filePath}");
        }
        catch (DirectoryNotFoundException)
        {
            throw new PluginManifestException($"清单文件目录不存在：{filePath}");
        }
        catch (IOException ex)
        {
            throw new PluginManifestException($"读取清单文件失败：{filePath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new PluginManifestException($"无权限访问清单文件：{filePath}", ex);
        }

        return Parse(json);
    }

    /// <summary>
    /// 获取必填字符串字段
    /// </summary>
    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            throw new PluginManifestException($"缺少必填字段：{propertyName}");
        }

        var value = element.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PluginManifestException($"必填字段不能为空：{propertyName}");
        }

        return value;
    }

    /// <summary>
    /// 获取可选字符串字段，带默认值
    /// </summary>
    private static string GetStringOrDefault(JsonElement root, string propertyName, string defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return defaultValue;
        }

        return element.GetString() ?? defaultValue;
    }

    /// <summary>
    /// 获取可空字符串字段
    /// </summary>
    private static string? GetStringOrNull(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.GetString();
    }

    /// <summary>
    /// 解析依赖列表
    /// </summary>
    private static IReadOnlyList<PluginDependency> ParseDependencies(JsonElement root)
    {
        if (!root.TryGetProperty("dependencies", out var depsElement))
        {
            return Array.Empty<PluginDependency>();
        }

        if (depsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PluginDependency>();
        }

        var dependencies = new List<PluginDependency>();

        foreach (var depElement in depsElement.EnumerateArray())
        {
            var pluginId = depElement.TryGetProperty("pluginId", out var idEl) ? idEl.GetString() ?? "" : "";
            var versionRange = depElement.TryGetProperty("versionRange", out var vrEl) ? vrEl.GetString() ?? "" : "";
            var isOptional = depElement.TryGetProperty("isOptional", out var optEl) && optEl.GetBoolean();

            dependencies.Add(new PluginDependency(pluginId, versionRange, isOptional));
        }

        return dependencies;
    }

    /// <summary>
    /// 解析权限标志
    /// </summary>
    private static PluginPermission ParsePermissions(JsonElement root)
    {
        if (!root.TryGetProperty("permissions", out var permElement))
        {
            return PluginPermission.None;
        }

        if (permElement.ValueKind == JsonValueKind.Number)
        {
            return (PluginPermission)permElement.GetInt32();
        }

        if (permElement.ValueKind == JsonValueKind.String)
        {
            var permStr = permElement.GetString() ?? "";

            if (Enum.TryParse<PluginPermission>(permStr, ignoreCase: true, out var result))
            {
                return result;
            }

            return ParsePermissionNames(permStr);
        }

        if (permElement.ValueKind == JsonValueKind.Array)
        {
            var combined = PluginPermission.None;

            foreach (var item in permElement.EnumerateArray())
            {
                var name = item.GetString() ?? "";

                if (Enum.TryParse<PluginPermission>(name, ignoreCase: true, out var perm))
                {
                    combined |= perm;
                }
            }

            return combined;
        }

        return PluginPermission.None;
    }

    /// <summary>
    /// 解析逗号分隔的权限名称
    /// </summary>
    private static PluginPermission ParsePermissionNames(string permStr)
    {
        var combined = PluginPermission.None;
        var parts = permStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (Enum.TryParse<PluginPermission>(part, ignoreCase: true, out var perm))
            {
                combined |= perm;
            }
        }

        return combined;
    }
}
