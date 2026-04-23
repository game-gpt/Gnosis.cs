using Gnosis.Core.Math;

namespace Gnosis.Navigation.Tile;

/// <summary>
/// 导航网格分块接口，支持大世界流式加载
/// </summary>
public interface INavMeshTile
{
    /// <summary>
    /// 分块坐标
    /// </summary>
    TileCoord Coord { get; }

    /// <summary>
    /// 是否已加载
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// 分块中的多边形数量
    /// </summary>
    int PolygonCount { get; }

    /// <summary>
    /// 分块边界（最小点）
    /// </summary>
    Vector3 BoundsMin { get; }

    /// <summary>
    /// 分块边界（最大点）
    /// </summary>
    Vector3 BoundsMax { get; }

    /// <summary>
    /// 加载分块数据
    /// </summary>
    void Load();

    /// <summary>
    /// 卸载分块数据
    /// </summary>
    void Unload();
}
