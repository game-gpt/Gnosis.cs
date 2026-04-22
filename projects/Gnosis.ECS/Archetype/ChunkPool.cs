using Gnosis.ECS.Component;

namespace Gnosis.ECS.Archetype;

/// <summary>
/// Chunk 内存池，减少频繁创建和销毁 Chunk 造成的 GC 压力。
/// 按 Archetype 的组件类型组合缓存可复用的 Chunk。
/// </summary>
public sealed class ChunkPool
{
    #region 字段

    private readonly Dictionary<long, Stack<Chunk>> _pool;
    private readonly ComponentTypeId _typeIdRegistry;
    private int _totalCount;
    private int _availableCount;

    #endregion

    #region 属性

    /// <summary>
    /// 池中 Chunk 总数（含已借出和可用的）
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// 可用 Chunk 数量
    /// </summary>
    public int AvailableCount => _availableCount;

    #endregion

    #region 构造函数

    public ChunkPool(ComponentTypeId typeIdRegistry)
    {
        _pool = new Dictionary<long, Stack<Chunk>>();
        _typeIdRegistry = typeIdRegistry;
        _totalCount = 0;
        _availableCount = 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从池中获取或创建一个 Chunk
    /// </summary>
    public Chunk Rent(IEnumerable<Type> componentTypes, int capacity = Chunk.DefaultCapacity)
    {
        var hash = ComputeHash(componentTypes);

        if (_pool.TryGetValue(hash, out var stack) && stack.Count > 0)
        {
            _availableCount--;
            return stack.Pop();
        }

        _totalCount++;
        return new Chunk(componentTypes, capacity);
    }

    /// <summary>
    /// 将 Chunk 归还到池中以供复用
    /// </summary>
    public void Return(Chunk chunk)
    {
        chunk.Clear();

        var hash = ComputeHash(chunk.ComponentTypes);

        if (!_pool.TryGetValue(hash, out var stack))
        {
            stack = new Stack<Chunk>();
            _pool[hash] = stack;
        }

        stack.Push(chunk);
        _availableCount++;
    }

    /// <summary>
    /// 清空池中所有缓存的 Chunk
    /// </summary>
    public void Clear()
    {
        _pool.Clear();
        _availableCount = 0;
        _totalCount = 0;
    }

    #endregion

    #region 私有方法

    private long ComputeHash(IEnumerable<Type> types)
    {
        long hash = 0;

        foreach (var type in types)
        {
            var id = _typeIdRegistry.GetOrRegister(type);
            hash = hash * 31 + id;
        }

        return hash;
    }

    #endregion
}
