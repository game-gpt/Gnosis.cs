using Gnosis.Core;

namespace Gnosis.ECS.Implementation;

public class ComponentPool<T> : ECS.IComponentPool where T : struct
{
    private readonly Dictionary<EntityId, int> _sparse;
    private readonly List<T> _dense;
    private readonly List<EntityId> _entities;

    public Type ComponentType => typeof(T);

    public int Count => _dense.Count;

    public ComponentPool()
    {
        _sparse = new Dictionary<EntityId, int>();
        _dense = [];
        _entities = [];
    }

    public void Add<TComponent>(EntityId entityId, TComponent component) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        if (_sparse.ContainsKey(entityId))
        {
            _dense[_sparse[entityId]] = (T)(object)component;
            return;
        }

        _sparse[entityId] = _dense.Count;
        _dense.Add((T)(object)component);
        _entities.Add(entityId);
    }

    public TComponent Get<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        if (!_sparse.TryGetValue(entityId, out var index))
        {
            throw new KeyNotFoundException($"实体 {entityId} 没有组件 {typeof(T).Name}");
        }

        return (TComponent)(object)_dense[index];
    }

    public void Remove<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        if (!_sparse.TryGetValue(entityId, out var index))
        {
            return;
        }

        var lastIndex = _dense.Count - 1;

        if (index != lastIndex)
        {
            _dense[index] = _dense[lastIndex];
            _entities[index] = _entities[lastIndex];
            _sparse[_entities[index]] = index;
        }

        _dense.RemoveAt(lastIndex);
        _entities.RemoveAt(lastIndex);
        _sparse.Remove(entityId);
    }

    public bool Has<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            return false;
        }

        return _sparse.ContainsKey(entityId);
    }

    public IReadOnlyList<T> GetAll()
    {
        return _dense;
    }

    public IReadOnlyList<EntityId> GetAllEntityIds()
    {
        return _entities;
    }
}
