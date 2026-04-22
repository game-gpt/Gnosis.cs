using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 版本兼容性检查器，验证插件间的版本兼容性
/// </summary>
public sealed class VersionCompatibilityChecker
{
    #region 字段

    private readonly string _engineApiVersion;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用引擎 API 版本初始化兼容性检查器
    /// </summary>
    /// <param name="engineApiVersion">当前引擎的 API 版本</param>
    public VersionCompatibilityChecker(string engineApiVersion)
    {
        if (string.IsNullOrWhiteSpace(engineApiVersion))
        {
            throw new ArgumentException("引擎 API 版本不能为空", nameof(engineApiVersion));
        }

        _engineApiVersion = engineApiVersion;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 检查插件清单与引擎 API 版本的兼容性
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>兼容性检查结果</returns>
    public CompatibilityResult CheckApiCompatibility(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(manifest.ApiVersion))
        {
            return CompatibilityResult.Incompatible(
                manifest.Id,
                "插件未声明 API 版本"
            );
        }

        if (!PluginVersion.TryParse(manifest.ApiVersion, out var pluginApiVersion))
        {
            return CompatibilityResult.Incompatible(
                manifest.Id,
                $"插件 API 版本格式无效：{manifest.ApiVersion}"
            );
        }

        if (!PluginVersion.TryParse(_engineApiVersion, out var engineApiVersion))
        {
            return CompatibilityResult.Incompatible(
                manifest.Id,
                $"引擎 API 版本格式无效：{_engineApiVersion}"
            );
        }

        if (pluginApiVersion!.Major != engineApiVersion!.Major)
        {
            return CompatibilityResult.Incompatible(
                manifest.Id,
                $"API 主版本不兼容：插件要求 {pluginApiVersion.Major}.x，引擎提供 {engineApiVersion.Major}.x"
            );
        }

        if (pluginApiVersion > engineApiVersion)
        {
            return CompatibilityResult.Incompatible(
                manifest.Id,
                $"插件要求更高版本的 API：{manifest.ApiVersion}，引擎提供：{_engineApiVersion}"
            );
        }

        return CompatibilityResult.Compatible(manifest.Id);
    }

    /// <summary>
    /// 检查两个插件之间的版本兼容性
    /// </summary>
    /// <param name="requiringManifest">需要依赖的插件清单</param>
    /// <param name="dependencyManifest">被依赖的插件清单</param>
    /// <returns>兼容性检查结果</returns>
    public CompatibilityResult CheckPluginCompatibility(
        PluginManifest requiringManifest,
        PluginManifest dependencyManifest)
    {
        ArgumentNullException.ThrowIfNull(requiringManifest);
        ArgumentNullException.ThrowIfNull(dependencyManifest);

        var dependency = requiringManifest.Dependencies
            .FirstOrDefault(d => string.Equals(d.PluginId, dependencyManifest.Id, StringComparison.OrdinalIgnoreCase));

        if (dependency is null)
        {
            return CompatibilityResult.Compatible(requiringManifest.Id);
        }

        if (!dependency.IsSatisfiedBy(dependencyManifest.Version))
        {
            return CompatibilityResult.Incompatible(
                requiringManifest.Id,
                $"插件 {requiringManifest.Id} 需要 {dependency.PluginId}@{dependency.VersionRange}，实际版本为 {dependencyManifest.Version}"
            );
        }

        return CompatibilityResult.Compatible(requiringManifest.Id);
    }

    /// <summary>
    /// 批量检查插件集合的兼容性
    /// </summary>
    /// <param name="manifests">插件清单集合</param>
    /// <returns>兼容性检查结果列表</returns>
    public IReadOnlyList<CompatibilityResult> CheckAll(IEnumerable<PluginManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);

        var results = new List<CompatibilityResult>();
        var manifestList = manifests.ToList();
        var manifestDict = manifestList.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifestList)
        {
            var apiResult = CheckApiCompatibility(manifest);
            results.Add(apiResult);

            if (!apiResult.IsCompatible)
            {
                continue;
            }

            foreach (var dep in manifest.Dependencies)
            {
                if (!manifestDict.TryGetValue(dep.PluginId, out var depManifest))
                {
                    if (!dep.IsOptional)
                    {
                        results.Add(CompatibilityResult.Incompatible(
                            manifest.Id,
                            $"缺少必需依赖：{dep.PluginId}@{dep.VersionRange}"
                        ));
                    }

                    continue;
                }

                var depResult = CheckPluginCompatibility(manifest, depManifest);
                results.Add(depResult);
            }
        }

        return results;
    }

    #endregion
}
