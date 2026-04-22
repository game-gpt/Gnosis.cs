using System;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件依赖描述
/// </summary>
public sealed class PluginDependency
{
    /// <summary>
    /// 依赖插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 版本范围
    /// </summary>
    public string VersionRange { get; }

    /// <summary>
    /// 是否为可选依赖
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// 初始化插件依赖
    /// </summary>
    /// <param name="pluginId">依赖插件标识</param>
    /// <param name="versionRange">版本范围</param>
    /// <param name="isOptional">是否为可选依赖</param>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    public PluginDependency(string pluginId, string versionRange, bool isOptional = false)
    {
        ArgumentNullException.ThrowIfNull(pluginId);
        ArgumentNullException.ThrowIfNull(versionRange);

        PluginId = pluginId;
        VersionRange = versionRange;
        IsOptional = isOptional;
    }

    /// <summary>
    /// 检查指定版本是否满足依赖的版本范围
    /// </summary>
    /// <param name="version">待检查的版本</param>
    /// <returns>是否满足版本范围</returns>
    public bool IsSatisfiedBy(PluginVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var range = VersionRange.Trim();

        if (range.StartsWith(">="))
        {
            var verStr = range[2..].Trim();
            return TryParseAndCompare(verStr, version, (v, min) => v >= min);
        }

        if (range.StartsWith("^"))
        {
            var verStr = range[1..].Trim();
            return IsCaretSatisfied(verStr, version);
        }

        if (range.StartsWith("~"))
        {
            var verStr = range[1..].Trim();
            return IsTildeSatisfied(verStr, version);
        }

        if (range.Contains('-'))
        {
            return IsRangeSatisfied(range, version);
        }

        return TryParseAndCompare(range, version, (v, target) => v == target);
    }

    /// <summary>
    /// 检查脱字符（^）版本范围是否满足
    /// </summary>
    private static bool IsCaretSatisfied(string verStr, PluginVersion version)
    {
        if (!PluginVersion.TryParse(verStr, out var target))
        {
            return false;
        }

        if (version.Major != target!.Major)
        {
            return false;
        }

        if (version.Major == 0)
        {
            if (version.Minor != target.Minor)
            {
                return false;
            }

            return version.Patch >= target.Patch;
        }

        if (version.Minor < target.Minor)
        {
            return false;
        }

        if (version.Minor == target.Minor)
        {
            return version.Patch >= target.Patch;
        }

        return true;
    }

    /// <summary>
    /// 检查波浪号（~）版本范围是否满足
    /// </summary>
    private static bool IsTildeSatisfied(string verStr, PluginVersion version)
    {
        if (!PluginVersion.TryParse(verStr, out var target))
        {
            return false;
        }

        if (version.Major != target!.Major)
        {
            return false;
        }

        if (version.Minor != target.Minor)
        {
            return false;
        }

        return version.Patch >= target.Patch;
    }

    /// <summary>
    /// 检查范围格式（如 "1.0.0-2.0.0"）是否满足
    /// </summary>
    private static bool IsRangeSatisfied(string range, PluginVersion version)
    {
        var parts = range.Split('-', 2);

        if (parts.Length != 2)
        {
            return false;
        }

        if (!PluginVersion.TryParse(parts[0].Trim(), out var min))
        {
            return false;
        }

        if (!PluginVersion.TryParse(parts[1].Trim(), out var max))
        {
            return false;
        }

        return version >= min && version <= max;
    }

    /// <summary>
    /// 尝试解析版本并比较
    /// </summary>
    private static bool TryParseAndCompare(string verStr, PluginVersion version, Func<PluginVersion, PluginVersion, bool> comparator)
    {
        if (!PluginVersion.TryParse(verStr, out var target))
        {
            return false;
        }

        return comparator(version, target!);
    }

    /// <summary>
    /// 返回依赖描述字符串
    /// </summary>
    public override string ToString()
    {
        var optional = IsOptional ? " (可选)" : "";
        return $"{PluginId}@{VersionRange}{optional}";
    }

    /// <summary>
    /// 判断两个依赖是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PluginDependency);
    }

    /// <summary>
    /// 判断两个依赖是否相等
    /// </summary>
    public bool Equals(PluginDependency? other)
    {
        if (other is null)
        {
            return false;
        }

        return PluginId == other.PluginId
               && VersionRange == other.VersionRange
               && IsOptional == other.IsOptional;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(PluginId, VersionRange, IsOptional);
    }
}
