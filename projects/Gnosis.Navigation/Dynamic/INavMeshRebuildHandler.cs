namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 导航网格重建处理器接口，处理动态障碍物引起的导航网格局部更新
/// </summary>
public interface INavMeshRebuildHandler
{
    /// <summary>
    /// 处理重建区域
    /// </summary>
    /// <param name="region">需要重建的区域</param>
    void HandleRebuild(NavMeshRebuildRegion region);

    /// <summary>
    /// 批量处理重建区域
    /// </summary>
    /// <param name="regions">需要重建的区域列表</param>
    void HandleRebuildBatch(IReadOnlyList<NavMeshRebuildRegion> regions);

    /// <summary>
    /// 是否正在重建
    /// </summary>
    bool IsRebuilding { get; }

    /// <summary>
    /// 待处理重建区域数量
    /// </summary>
    int PendingRegionCount { get; }
}
