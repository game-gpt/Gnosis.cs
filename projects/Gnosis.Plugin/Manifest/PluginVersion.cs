using System;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 语义版本号数据模型，遵循 SemVer 2.0 规范
/// </summary>
public sealed class PluginVersion : IComparable<PluginVersion>
{
    /// <summary>
    /// 主版本号
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// 次版本号
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// 修订号
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// 预发布标识
    /// </summary>
    public string? Prerelease { get; }

    /// <summary>
    /// 构建元数据
    /// </summary>
    public string? Build { get; }

    /// <summary>
    /// 初始化语义版本号
    /// </summary>
    public PluginVersion(int major, int minor, int patch, string? prerelease = null, string? build = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        Build = build;
    }

    /// <summary>
    /// 解析版本字符串为语义版本号实例
    /// </summary>
    /// <param name="version">版本字符串，格式如 "1.2.3-alpha.1+build.123"</param>
    /// <returns>解析后的语义版本号</returns>
    /// <exception cref="FormatException">版本字符串格式无效时抛出</exception>
    public static PluginVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new FormatException("版本字符串不能为空");
        }

        if (!TryParse(version, out var result))
        {
            throw new FormatException($"版本字符串格式无效：{version}");
        }

        return result!;
    }

    /// <summary>
    /// 尝试解析版本字符串为语义版本号实例
    /// </summary>
    /// <param name="version">版本字符串</param>
    /// <param name="result">解析结果</param>
    /// <returns>解析是否成功</returns>
    public static bool TryParse(string version, out PluginVersion? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var span = version.AsSpan();

        var buildIndex = span.IndexOf('+');
        ReadOnlySpan<char> versionAndPreRelease = buildIndex >= 0 ? span.Slice(0, buildIndex) : span;
        var build = buildIndex >= 0 ? span.Slice(buildIndex + 1).ToString() : null;

        var prereleaseIndex = versionAndPreRelease.IndexOf('-');
        ReadOnlySpan<char> versionPart = prereleaseIndex >= 0 ? versionAndPreRelease.Slice(0, prereleaseIndex) : versionAndPreRelease;
        var prerelease = prereleaseIndex >= 0 ? versionAndPreRelease.Slice(prereleaseIndex + 1).ToString() : null;

        var parts = versionPart.Split('.');
        var partList = new List<int>();

        foreach (var range in parts)
        {
            var part = versionPart[range];

            if (!int.TryParse(part, out var num) || num < 0)
            {
                return false;
            }

            partList.Add(num);
        }

        if (partList.Count != 3)
        {
            return false;
        }

        result = new PluginVersion(partList[0], partList[1], partList[2], prerelease, build);
        return true;
    }

    /// <summary>
    /// 返回完整版本字符串
    /// </summary>
    public override string ToString()
    {
        var result = $"{Major}.{Minor}.{Patch}";

        if (!string.IsNullOrEmpty(Prerelease))
        {
            result = $"{result}-{Prerelease}";
        }

        if (!string.IsNullOrEmpty(Build))
        {
            result = $"{result}+{Build}";
        }

        return result;
    }

    /// <summary>
    /// 按语义版本规则比较两个版本号
    /// </summary>
    public int CompareTo(PluginVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        if (Patch != other.Patch)
        {
            return Patch.CompareTo(other.Patch);
        }

        var thisHasPrerelease = !string.IsNullOrEmpty(Prerelease);
        var otherHasPrerelease = !string.IsNullOrEmpty(other.Prerelease);

        if (!thisHasPrerelease && otherHasPrerelease)
        {
            return 1;
        }

        if (thisHasPrerelease && !otherHasPrerelease)
        {
            return -1;
        }

        if (thisHasPrerelease && otherHasPrerelease)
        {
            return ComparePrerelease(Prerelease!, other.Prerelease!);
        }

        return 0;
    }

    /// <summary>
    /// 比较预发布标识
    /// </summary>
    private static int ComparePrerelease(string left, string right)
    {
        var leftParts = left.Split('.');
        var rightParts = right.Split('.');

        var maxLen = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < maxLen; i++)
        {
            if (i >= leftParts.Length)
            {
                return -1;
            }

            if (i >= rightParts.Length)
            {
                return 1;
            }

            var leftIsNum = int.TryParse(leftParts[i], out var leftNum);
            var rightIsNum = int.TryParse(rightParts[i], out var rightNum);

            if (leftIsNum && rightIsNum)
            {
                if (leftNum != rightNum)
                {
                    return leftNum.CompareTo(rightNum);
                }

                continue;
            }

            if (leftIsNum)
            {
                return -1;
            }

            if (rightIsNum)
            {
                return 1;
            }

            var cmp = string.Compare(leftParts[i], rightParts[i], StringComparison.Ordinal);

            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    /// <summary>
    /// 判断两个版本号是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PluginVersion);
    }

    /// <summary>
    /// 判断两个版本号是否相等
    /// </summary>
    public bool Equals(PluginVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Major == other.Major
               && Minor == other.Minor
               && Patch == other.Patch
               && Prerelease == other.Prerelease
               && Build == other.Build;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, Prerelease, Build);
    }

    public static bool operator ==(PluginVersion? left, PluginVersion? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(PluginVersion? left, PluginVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(PluginVersion? left, PluginVersion? right)
    {
        if (left is null)
        {
            return right is not null;
        }

        return left.CompareTo(right) < 0;
    }

    public static bool operator >(PluginVersion? left, PluginVersion? right)
    {
        if (left is null)
        {
            return false;
        }

        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(PluginVersion? left, PluginVersion? right)
    {
        if (left is null)
        {
            return true;
        }

        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(PluginVersion? left, PluginVersion? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.CompareTo(right) >= 0;
    }
}
