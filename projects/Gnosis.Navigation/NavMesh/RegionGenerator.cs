namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 区域生成器，从体素化数据中生成连通的可行走区域
/// </summary>
public class RegionGenerator
{
    #region 内部类型

    /// <summary>
    /// 连通区域
    /// </summary>
    private class Region
    {
        public int Id;
        public int Area;
        public List<int> Spans = new();
    }

    #endregion

    #region 字段

    private int _width;
    private int _depth;
    private int[] _regionMap = [];
    private List<Region> _regions = [];

    #endregion

    #region 属性

    /// <summary>
    /// 生成的区域数量
    /// </summary>
    public int RegionCount => _regions?.Count ?? 0;

    /// <summary>
    /// 区域映射表（体素坐标到区域 ID）
    /// </summary>
    public int[] RegionMap => _regionMap;

    #endregion

    #region 公开方法

    /// <summary>
    /// 从体素化器生成区域
    /// </summary>
    public void Generate(Voxelizer voxelizer, float regionMinArea)
    {
        _width = voxelizer.Width;
        _depth = voxelizer.Depth;
        _regionMap = new int[_width * _depth];
        _regions = new List<Region>();

        for (var i = 0; i < _regionMap.Length; i++)
        {
            _regionMap[i] = -1;
        }

        FloodFillRegions(voxelizer);

        MergeSmallRegions(regionMinArea, voxelizer);

        CompactRegionIds();
    }

    /// <summary>
    /// 获取指定体素位置的区域 ID
    /// </summary>
    public int GetRegionId(int x, int z)
    {
        if (x < 0 || x >= _width || z < 0 || z >= _depth)
        {
            return -1;
        }
        return _regionMap[z * _width + x];
    }

    /// <summary>
    /// 获取指定区域的体素数量
    /// </summary>
    public int GetRegionArea(int regionId)
    {
        foreach (var region in _regions)
        {
            if (region.Id == regionId)
            {
                return region.Area;
            }
        }
        return 0;
    }

    /// <summary>
    /// 获取指定区域的轮廓点
    /// </summary>
    public List<float[]> GetRegionContour(int regionId, Voxelizer voxelizer)
    {
        var contour = new List<float[]>();
        var visited = new HashSet<int>();

        for (var z = 0; z < _depth; z++)
        {
            for (var x = 0; x < _width; x++)
            {
                var idx = z * _width + x;
                if (_regionMap[idx] != regionId || visited.Contains(idx))
                {
                    continue;
                }

                if (IsBorderVoxel(x, z, regionId))
                {
                    TraceContour(x, z, regionId, voxelizer, contour, visited);
                    return contour;
                }
            }
        }

        return contour;
    }

    #endregion

    #region 私有方法

    private void FloodFillRegions(Voxelizer voxelizer)
    {
        var currentId = 0;

        for (var z = 0; z < _depth; z++)
        {
            for (var x = 0; x < _width; x++)
            {
                var idx = z * _width + x;
                if (_regionMap[idx] != -1)
                {
                    continue;
                }

                if (!voxelizer.GetWalkableSpan(x, z, out _, out _))
                {
                    continue;
                }

                var region = new Region
                {
                    Id = currentId,
                    Area = 0
                };

                FloodFill(x, z, currentId, region, voxelizer);

                _regions.Add(region);
                currentId++;
            }
        }
    }

    private void FloodFill(int startX, int startZ, int regionId, Region region, Voxelizer voxelizer)
    {
        var stack = new Stack<(int x, int z)>();
        stack.Push((startX, startZ));

        while (stack.Count > 0)
        {
            var (x, z) = stack.Pop();
            var idx = z * _width + x;

            if (x < 0 || x >= _width || z < 0 || z >= _depth)
            {
                continue;
            }

            if (_regionMap[idx] != -1)
            {
                continue;
            }

            if (!voxelizer.GetWalkableSpan(x, z, out _, out _))
            {
                continue;
            }

            _regionMap[idx] = regionId;
            region.Area++;
            region.Spans.Add(idx);

            stack.Push((x + 1, z));
            stack.Push((x - 1, z));
            stack.Push((x, z + 1));
            stack.Push((x, z - 1));
        }
    }

    private void MergeSmallRegions(float regionMinArea, Voxelizer voxelizer)
    {
        var minAreaVoxels = (int)(regionMinArea / (voxelizer.VoxelSize * voxelizer.VoxelSize));
        var merged = true;

        while (merged)
        {
            merged = false;

            foreach (var region in _regions)
            {
                if (region.Area >= minAreaVoxels || region.Id == -1)
                {
                    continue;
                }

                var bestNeighbor = FindLargestNeighborRegion(region);
                if (bestNeighbor != null)
                {
                    MergeRegionInto(region, bestNeighbor);
                    merged = true;
                }
            }
        }
    }

    private Region FindLargestNeighborRegion(Region region)
    {
        Region? bestNeighbor = null;
        var bestArea = 0;

        foreach (var spanIdx in region.Spans)
        {
            var x = spanIdx % _width;
            var z = spanIdx / _width;

            var neighbors = new (int dx, int dz)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

            foreach (var (dx, dz) in neighbors)
            {
                var nx = x + dx;
                var nz = z + dz;

                if (nx < 0 || nx >= _width || nz < 0 || nz >= _depth)
                {
                    continue;
                }

                var neighborRegionId = _regionMap[nz * _width + nx];
                if (neighborRegionId == region.Id || neighborRegionId == -1)
                {
                    continue;
                }

                var neighborRegion = _regions.Find(r => r.Id == neighborRegionId);
                if (neighborRegion != null && neighborRegion.Area > bestArea)
                {
                    bestNeighbor = neighborRegion;
                    bestArea = neighborRegion.Area;
                }
            }
        }

        return bestNeighbor!;
    }

    private void MergeRegionInto(Region source, Region target)
    {
        foreach (var spanIdx in source.Spans)
        {
            _regionMap[spanIdx] = target.Id;
            target.Spans.Add(spanIdx);
        }

        target.Area += source.Area;
        source.Id = -1;
        source.Area = 0;
        source.Spans.Clear();
    }

    private void CompactRegionIds()
    {
        var idMap = new Dictionary<int, int>();
        var newId = 0;

        foreach (var region in _regions)
        {
            if (region.Id == -1)
            {
                continue;
            }

            idMap[region.Id] = newId;
            region.Id = newId;
            newId++;
        }

        for (var i = 0; i < _regionMap.Length; i++)
        {
            if (_regionMap[i] != -1 && idMap.TryGetValue(_regionMap[i], out var mappedId))
            {
                _regionMap[i] = mappedId;
            }
        }

        _regions.RemoveAll(r => r.Id == -1);
    }

    private bool IsBorderVoxel(int x, int z, int regionId)
    {
        var neighbors = new (int dx, int dz)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

        foreach (var (dx, dz) in neighbors)
        {
            var nx = x + dx;
            var nz = z + dz;

            if (nx < 0 || nx >= _width || nz < 0 || nz >= _depth)
            {
                return true;
            }

            if (_regionMap[nz * _width + nx] != regionId)
            {
                return true;
            }
        }

        return false;
    }

    private void TraceContour(int startX, int startZ, int regionId, Voxelizer voxelizer, List<float[]> contour, HashSet<int> visited)
    {
        var x = startX;
        var z = startZ;
        var dir = 0;

        var dxArr = new[] { 1, 0, -1, 0 };
        var dzArr = new[] { 0, 1, 0, -1 };

        var maxSteps = _width * _depth * 4;
        var steps = 0;

        do
        {
            var idx = z * _width + x;
            visited.Add(idx);

            voxelizer.GetWalkableSpan(x, z, out var yMin, out _);
            var worldPos = voxelizer.VoxelToWorld(x, yMin, z);
            contour.Add(worldPos);

            var found = false;
            for (var i = 0; i < 4; i++)
            {
                var checkDir = (dir + i) % 4;
                var nx = x + dxArr[checkDir];
                var nz = z + dzArr[checkDir];

                if (nx < 0 || nx >= _width || nz < 0 || nz >= _depth)
                {
                    continue;
                }

                if (_regionMap[nz * _width + nx] == regionId)
                {
                    x = nx;
                    z = nz;
                    dir = checkDir;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                break;
            }

            steps++;
            if (steps > maxSteps)
            {
                break;
            }

        } while (x != startX || z != startZ);
    }

    #endregion
}
