using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Tile;

/// <summary>
/// 分块导航管理器，管理导航网格分块的加载、卸载和流式调度
/// </summary>
public sealed class TileManager
{
    #region 字段

    private readonly Dictionary<TileCoord, NavMeshTile> _tiles = new();
    private readonly List<NavMeshTile> _loadedTiles = new();
    private Vector3 _viewerPosition;
    private float _loadRadius;
    private float _unloadRadius;

    #endregion

    #region 属性

    /// <summary>
    /// 所有分块
    /// </summary>
    public IReadOnlyDictionary<TileCoord, NavMeshTile> Tiles => _tiles;

    /// <summary>
    /// 已加载的分块
    /// </summary>
    public IReadOnlyList<NavMeshTile> LoadedTiles => _loadedTiles;

    /// <summary>
    /// 分块总数
    /// </summary>
    public int TotalTileCount => _tiles.Count;

    /// <summary>
    /// 已加载分块数量
    /// </summary>
    public int LoadedTileCount => _loadedTiles.Count;

    /// <summary>
    /// 观察者位置（用于流式加载决策）
    /// </summary>
    public Vector3 ViewerPosition
    {
        get => _viewerPosition;
        set => _viewerPosition = value;
    }

    /// <summary>
    /// 加载半径
    /// </summary>
    public float LoadRadius
    {
        get => _loadRadius;
        set => _loadRadius = Math.Max(1.0f, value);
    }

    /// <summary>
    /// 卸载半径
    /// </summary>
    public float UnloadRadius
    {
        get => _unloadRadius;
        set => _unloadRadius = Math.Max(_loadRadius, value);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建分块管理器
    /// </summary>
    /// <param name="loadRadius">加载半径</param>
    /// <param name="unloadRadius">卸载半径</param>
    public TileManager(float loadRadius = 100.0f, float unloadRadius = 150.0f)
    {
        _loadRadius = loadRadius;
        _unloadRadius = unloadRadius;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 注册分块
    /// </summary>
    /// <param name="tile">导航网格分块</param>
    public void RegisterTile(NavMeshTile tile)
    {
        _tiles[tile.Coord] = tile;
    }

    /// <summary>
    /// 注销分块
    /// </summary>
    /// <param name="coord">分块坐标</param>
    public void UnregisterTile(TileCoord coord)
    {
        if (_tiles.TryGetValue(coord, out var tile))
        {
            if (tile.IsLoaded)
            {
                tile.Unload();
                _loadedTiles.Remove(tile);
            }

            _tiles.Remove(coord);
        }
    }

    /// <summary>
    /// 获取指定坐标的分块
    /// </summary>
    /// <param name="coord">分块坐标</param>
    /// <returns>分块，不存在则返回 null</returns>
    public NavMeshTile? GetTile(TileCoord coord)
    {
        return _tiles.GetValueOrDefault(coord);
    }

    /// <summary>
    /// 根据世界坐标查找所在分块
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>所在分块，不存在则返回 null</returns>
    public NavMeshTile? FindTileAtPosition(Vector3 position)
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile.ContainsPoint(position))
            {
                return tile;
            }
        }

        return null;
    }

    /// <summary>
    /// 更新流式加载，根据观察者位置加载/卸载分块
    /// </summary>
    public void UpdateStreaming()
    {
        var loadRadiusSq = _loadRadius * _loadRadius;
        var unloadRadiusSq = _unloadRadius * _unloadRadius;

        foreach (var tile in _tiles.Values)
        {
            var distSq = DistanceToTile(tile);

            if (!tile.IsLoaded && distSq <= loadRadiusSq)
            {
                tile.Load();
                _loadedTiles.Add(tile);
            }
            else if (tile.IsLoaded && distSq > unloadRadiusSq)
            {
                tile.Unload();
                _loadedTiles.Remove(tile);
            }
        }
    }

    /// <summary>
    /// 强制加载指定分块
    /// </summary>
    /// <param name="coord">分块坐标</param>
    public void ForceLoad(TileCoord coord)
    {
        if (_tiles.TryGetValue(coord, out var tile) && !tile.IsLoaded)
        {
            tile.Load();
            _loadedTiles.Add(tile);
        }
    }

    /// <summary>
    /// 强制卸载指定分块
    /// </summary>
    /// <param name="coord">分块坐标</param>
    public void ForceUnload(TileCoord coord)
    {
        if (_tiles.TryGetValue(coord, out var tile) && tile.IsLoaded)
        {
            tile.Unload();
            _loadedTiles.Remove(tile);
        }
    }

    /// <summary>
    /// 卸载所有分块
    /// </summary>
    public void UnloadAll()
    {
        foreach (var tile in _loadedTiles)
        {
            tile.Unload();
        }

        _loadedTiles.Clear();
    }

    /// <summary>
    /// 清空所有分块
    /// </summary>
    public void Clear()
    {
        UnloadAll();
        _tiles.Clear();
    }

    #endregion

    #region 私有方法

    private float DistanceToTile(NavMeshTile tile)
    {
        var closestX = Math.Max(tile.BoundsMin.X, Math.Min(_viewerPosition.X, tile.BoundsMax.X));
        var closestY = Math.Max(tile.BoundsMin.Y, Math.Min(_viewerPosition.Y, tile.BoundsMax.Y));
        var closestZ = Math.Max(tile.BoundsMin.Z, Math.Min(_viewerPosition.Z, tile.BoundsMax.Z));

        var dx = _viewerPosition.X - closestX;
        var dy = _viewerPosition.Y - closestY;
        var dz = _viewerPosition.Z - closestZ;

        return dx * dx + dy * dy + dz * dz;
    }

    #endregion
}
