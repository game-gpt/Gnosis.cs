namespace Gnosis.ECS.Entity;

/// <summary>
/// 实体管理器，负责实体的创建、销毁和生命周期管理。
/// 使用代际机制确保已销毁实体的旧引用不会被误用。
/// </summary>
public sealed class EntityManager
{
    private uint[] _generations;
    private bool[] _alive;
    private readonly Queue<uint> _freeIndices;
    private uint _nextIndex;
    private int _aliveCount;

    /// <summary>
    /// 当前活跃实体数量
    /// </summary>
    public int AliveCount => _aliveCount;

    /// <summary>
    /// 已分配的实体槽位总数（含已销毁的）
    /// </summary>
    public int Capacity => _generations.Length;

    public EntityManager(int initialCapacity = 1024)
    {
        _generations = new uint[initialCapacity];
        _alive = new bool[initialCapacity];
        _freeIndices = new Queue<uint>();
        _nextIndex = 1;
        _aliveCount = 0;
    }

    /// <summary>
    /// 创建一个新实体，返回其 EntityId
    /// </summary>
    public EntityId CreateEntity()
    {
        uint index;

        if (_freeIndices.Count > 0)
        {
            index = _freeIndices.Dequeue();
        }
        else
        {
            index = _nextIndex++;

            if (index >= _generations.Length)
            {
                GrowArrays((int)(index * 2));
            }
        }

        _alive[index] = true;
        _aliveCount++;

        return new EntityId(index, _generations[index]);
    }

    /// <summary>
    /// 批量创建多个实体
    /// </summary>
    public EntityId[] CreateEntities(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "创建数量不能为负数");
        }

        var entities = new EntityId[count];

        for (var i = 0; i < count; i++)
        {
            entities[i] = CreateEntity();
        }

        return entities;
    }

    /// <summary>
    /// 销毁指定实体，其索引将被回收复用
    /// </summary>
    public bool DestroyEntity(EntityId entityId)
    {
        if (!IsAlive(entityId))
        {
            return false;
        }

        _alive[entityId.Index] = false;
        _generations[entityId.Index]++;
        _aliveCount--;
        _freeIndices.Enqueue(entityId.Index);

        return true;
    }

    /// <summary>
    /// 检查指定实体是否存活（代际校验）
    /// </summary>
    public bool IsAlive(EntityId entityId)
    {
        if (entityId.Index == 0 || entityId.Index >= _nextIndex)
        {
            return false;
        }

        return _alive[entityId.Index] && _generations[entityId.Index] == entityId.Generation;
    }

    /// <summary>
    /// 获取指定实体的当前代际
    /// </summary>
    public uint GetGeneration(uint index)
    {
        if (index >= _generations.Length)
        {
            return 0;
        }

        return _generations[index];
    }

    /// <summary>
    /// 获取所有存活实体的 ID 列表。
    /// 仅遍历已使用的索引范围，跳过已销毁的槽位。
    /// </summary>
    public List<EntityId> GetAllAliveEntities()
    {
        var result = new List<EntityId>(_aliveCount);

        for (uint i = 1; i < _nextIndex; i++)
        {
            if (_alive[i])
            {
                result.Add(new EntityId(i, _generations[i]));
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有存活实体的 ID 列表，使用调用者提供的列表以避免分配。
    /// </summary>
    public void GetAllAliveEntities(List<EntityId> result)
    {
        result.Clear();

        for (uint i = 1; i < _nextIndex; i++)
        {
            if (_alive[i])
            {
                result.Add(new EntityId(i, _generations[i]));
            }
        }
    }

    private void GrowArrays(int newCapacity)
    {
        var newGenerations = new uint[newCapacity];
        var newAlive = new bool[newCapacity];

        Array.Copy(_generations, newGenerations, _generations.Length);
        Array.Copy(_alive, newAlive, _alive.Length);

        _generations = newGenerations;
        _alive = newAlive;
    }
}
