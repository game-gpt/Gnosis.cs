using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;
using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.System;

/// <summary>
/// 内联系统基类，直接遍历 Archetype 的 Chunk 数据，
/// 消除查询构建和迭代器分配开销。
/// 适用于高频帧更新系统（如 MovementSystem、PhysicsSystem）。
/// 通过泛型特化使 JIT 能够内联 Execute 回调，消除虚调用开销。
/// </summary>
public abstract class InlineSystem<T1> : IWorldSystem
    where T1 : struct
{
    private World.World? _world;
    private List<ArchetypeEntity>? _cachedArchetypes;
    private int _cachedSlot1 = -1;
    private int _lastArchetypeCount = -1;

    /// <summary>
    /// 系统执行阶段
    /// </summary>
    public abstract SystemPhase Phase { get; }

    /// <summary>
    /// 设置系统所属的 World
    /// </summary>
    public void SetWorld(World.World world)
    {
        _world = world;
        InvalidateCache();
    }

    /// <summary>
    /// 系统帧更新，直接遍历 Archetype Chunk 数据
    /// </summary>
    public void Update(float delta)
    {
        EnsureCache();

        if (_cachedArchetypes == null || _cachedSlot1 < 0)
        {
            return;
        }

        foreach (var archetype in _cachedArchetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(_cachedSlot1);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    Execute(entityArray[i], ref comp1Array[i], delta);
                }
            }
        }
    }

    /// <summary>
    /// 对每个匹配实体执行的逻辑（由子类实现，JIT 可内联）
    /// </summary>
    protected abstract void Execute(EntityId entity, ref T1 comp1, float delta);

    public virtual void Initialize() { }

    public virtual void Shutdown() { }

    /// <summary>
    /// 使缓存失效，下次 Update 时重新查询
    /// </summary>
    protected void InvalidateCache()
    {
        _cachedArchetypes = null;
        _cachedSlot1 = -1;
        _lastArchetypeCount = -1;
    }

    private void EnsureCache()
    {
        if (_world == null)
        {
            return;
        }

        var currentCount = _world.Archetypes.ArchetypeCount;

        if (_cachedArchetypes != null && _lastArchetypeCount == currentCount)
        {
            return;
        }

        var allTypes = new HashSet<Type> { typeof(T1) };
        var empty = new HashSet<Type>();
        _cachedArchetypes = _world.Archetypes.QueryArchetypes(allTypes, empty, empty).ToList();

        if (_cachedArchetypes.Count > 0)
        {
            _cachedSlot1 = _cachedArchetypes[0].GetComponentSlot<T1>();
        }

        _lastArchetypeCount = currentCount;
    }
}

/// <summary>
/// 双组件内联系统基类
/// </summary>
public abstract class InlineSystem<T1, T2> : IWorldSystem
    where T1 : struct
    where T2 : struct
{
    private World.World? _world;
    private List<ArchetypeEntity>? _cachedArchetypes;
    private int _cachedSlot1 = -1;
    private int _cachedSlot2 = -1;
    private int _lastArchetypeCount = -1;

    /// <summary>
    /// 系统执行阶段
    /// </summary>
    public abstract SystemPhase Phase { get; }

    /// <summary>
    /// 设置系统所属的 World
    /// </summary>
    public void SetWorld(World.World world)
    {
        _world = world;
        InvalidateCache();
    }

    /// <summary>
    /// 系统帧更新，直接遍历 Archetype Chunk 数据
    /// </summary>
    public void Update(float delta)
    {
        EnsureCache();

        if (_cachedArchetypes == null || _cachedSlot1 < 0 || _cachedSlot2 < 0)
        {
            return;
        }

        foreach (var archetype in _cachedArchetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(_cachedSlot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(_cachedSlot2);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    Execute(entityArray[i], ref comp1Array[i], ref comp2Array[i], delta);
                }
            }
        }
    }

    /// <summary>
    /// 对每个匹配实体执行的逻辑（由子类实现，JIT 可内联）
    /// </summary>
    protected abstract void Execute(EntityId entity, ref T1 comp1, ref T2 comp2, float delta);

    public virtual void Initialize() { }

    public virtual void Shutdown() { }

    /// <summary>
    /// 使缓存失效，下次 Update 时重新查询
    /// </summary>
    protected void InvalidateCache()
    {
        _cachedArchetypes = null;
        _cachedSlot1 = -1;
        _cachedSlot2 = -1;
        _lastArchetypeCount = -1;
    }

    private void EnsureCache()
    {
        if (_world == null)
        {
            return;
        }

        var currentCount = _world.Archetypes.ArchetypeCount;

        if (_cachedArchetypes != null && _lastArchetypeCount == currentCount)
        {
            return;
        }

        var allTypes = new HashSet<Type> { typeof(T1), typeof(T2) };
        var empty = new HashSet<Type>();
        _cachedArchetypes = _world.Archetypes.QueryArchetypes(allTypes, empty, empty).ToList();

        if (_cachedArchetypes.Count > 0)
        {
            _cachedSlot1 = _cachedArchetypes[0].GetComponentSlot<T1>();
            _cachedSlot2 = _cachedArchetypes[0].GetComponentSlot<T2>();
        }

        _lastArchetypeCount = currentCount;
    }
}

/// <summary>
/// 三组件内联系统基类
/// </summary>
public abstract class InlineSystem<T1, T2, T3> : IWorldSystem
    where T1 : struct
    where T2 : struct
    where T3 : struct
{
    private World.World? _world;
    private List<ArchetypeEntity>? _cachedArchetypes;
    private int _cachedSlot1 = -1;
    private int _cachedSlot2 = -1;
    private int _cachedSlot3 = -1;
    private int _lastArchetypeCount = -1;

    /// <summary>
    /// 系统执行阶段
    /// </summary>
    public abstract SystemPhase Phase { get; }

    /// <summary>
    /// 设置系统所属的 World
    /// </summary>
    public void SetWorld(World.World world)
    {
        _world = world;
        InvalidateCache();
    }

    /// <summary>
    /// 系统帧更新，直接遍历 Archetype Chunk 数据
    /// </summary>
    public void Update(float delta)
    {
        EnsureCache();

        if (_cachedArchetypes == null || _cachedSlot1 < 0 || _cachedSlot2 < 0 || _cachedSlot3 < 0)
        {
            return;
        }

        foreach (var archetype in _cachedArchetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(_cachedSlot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(_cachedSlot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(_cachedSlot3);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    Execute(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i], delta);
                }
            }
        }
    }

    /// <summary>
    /// 对每个匹配实体执行的逻辑（由子类实现，JIT 可内联）
    /// </summary>
    protected abstract void Execute(EntityId entity, ref T1 comp1, ref T2 comp2, ref T3 comp3, float delta);

    public virtual void Initialize() { }

    public virtual void Shutdown() { }

    /// <summary>
    /// 使缓存失效，下次 Update 时重新查询
    /// </summary>
    protected void InvalidateCache()
    {
        _cachedArchetypes = null;
        _cachedSlot1 = -1;
        _cachedSlot2 = -1;
        _cachedSlot3 = -1;
        _lastArchetypeCount = -1;
    }

    private void EnsureCache()
    {
        if (_world == null)
        {
            return;
        }

        var currentCount = _world.Archetypes.ArchetypeCount;

        if (_cachedArchetypes != null && _lastArchetypeCount == currentCount)
        {
            return;
        }

        var allTypes = new HashSet<Type> { typeof(T1), typeof(T2), typeof(T3) };
        var empty = new HashSet<Type>();
        _cachedArchetypes = _world.Archetypes.QueryArchetypes(allTypes, empty, empty).ToList();

        if (_cachedArchetypes.Count > 0)
        {
            _cachedSlot1 = _cachedArchetypes[0].GetComponentSlot<T1>();
            _cachedSlot2 = _cachedArchetypes[0].GetComponentSlot<T2>();
            _cachedSlot3 = _cachedArchetypes[0].GetComponentSlot<T3>();
        }

        _lastArchetypeCount = currentCount;
    }
}

/// <summary>
/// 四组件内联系统基类
/// </summary>
public abstract class InlineSystem<T1, T2, T3, T4> : IWorldSystem
    where T1 : struct
    where T2 : struct
    where T3 : struct
    where T4 : struct
{
    private World.World? _world;
    private List<ArchetypeEntity>? _cachedArchetypes;
    private int _cachedSlot1 = -1;
    private int _cachedSlot2 = -1;
    private int _cachedSlot3 = -1;
    private int _cachedSlot4 = -1;
    private int _lastArchetypeCount = -1;

    /// <summary>
    /// 系统执行阶段
    /// </summary>
    public abstract SystemPhase Phase { get; }

    /// <summary>
    /// 设置系统所属的 World
    /// </summary>
    public void SetWorld(World.World world)
    {
        _world = world;
        InvalidateCache();
    }

    /// <summary>
    /// 系统帧更新，直接遍历 Archetype Chunk 数据
    /// </summary>
    public void Update(float delta)
    {
        EnsureCache();

        if (_cachedArchetypes == null || _cachedSlot1 < 0 || _cachedSlot2 < 0 ||
            _cachedSlot3 < 0 || _cachedSlot4 < 0)
        {
            return;
        }

        foreach (var archetype in _cachedArchetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(_cachedSlot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(_cachedSlot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(_cachedSlot3);
                var comp4Array = chunk.GetComponentArrayBySlot<T4>(_cachedSlot4);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    Execute(entityArray[i], ref comp1Array[i], ref comp2Array[i],
                        ref comp3Array[i], ref comp4Array[i], delta);
                }
            }
        }
    }

    /// <summary>
    /// 对每个匹配实体执行的逻辑（由子类实现，JIT 可内联）
    /// </summary>
    protected abstract void Execute(EntityId entity, ref T1 comp1, ref T2 comp2,
        ref T3 comp3, ref T4 comp4, float delta);

    public virtual void Initialize() { }

    public virtual void Shutdown() { }

    /// <summary>
    /// 使缓存失效，下次 Update 时重新查询
    /// </summary>
    protected void InvalidateCache()
    {
        _cachedArchetypes = null;
        _cachedSlot1 = -1;
        _cachedSlot2 = -1;
        _cachedSlot3 = -1;
        _cachedSlot4 = -1;
        _lastArchetypeCount = -1;
    }

    private void EnsureCache()
    {
        if (_world == null)
        {
            return;
        }

        var currentCount = _world.Archetypes.ArchetypeCount;

        if (_cachedArchetypes != null && _lastArchetypeCount == currentCount)
        {
            return;
        }

        var allTypes = new HashSet<Type> { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
        var empty = new HashSet<Type>();
        _cachedArchetypes = _world.Archetypes.QueryArchetypes(allTypes, empty, empty).ToList();

        if (_cachedArchetypes.Count > 0)
        {
            _cachedSlot1 = _cachedArchetypes[0].GetComponentSlot<T1>();
            _cachedSlot2 = _cachedArchetypes[0].GetComponentSlot<T2>();
            _cachedSlot3 = _cachedArchetypes[0].GetComponentSlot<T3>();
            _cachedSlot4 = _cachedArchetypes[0].GetComponentSlot<T4>();
        }

        _lastArchetypeCount = currentCount;
    }
}
