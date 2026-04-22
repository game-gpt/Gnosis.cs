using Gnosis.ECS.Core;

namespace Gnosis.Testing.Mocker;

public class MockWorld : ECS.Interface.IWorld
{
    #region Fields

    private readonly Dictionary<EntityId, Dictionary<Type, object>> _components = new();
    private int _entityCount;

    #endregion

    #region Properties

    public int EntityCount => _components.Count;

    #endregion

    #region Public Methods

    public EntityId CreateEntity()
    {
        var id = EntityId.New();
        _components[id] = new Dictionary<Type, object>();
        _entityCount++;
        return id;
    }

    public void DestroyEntity(EntityId entityId)
    {
        if (_components.Remove(entityId))
        {
            _entityCount--;
        }
    }

    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (_components.TryGetValue(entityId, out var components))
        {
            components[typeof(T)] = component;
        }
    }

    public T GetComponent<T>(EntityId entityId) where T : struct
    {
        if (_components.TryGetValue(entityId, out var components) &&
            components.TryGetValue(typeof(T), out var component))
        {
            return (T)component;
        }

        return default;
    }

    public bool HasComponent<T>(EntityId entityId) where T : struct
    {
        return _components.TryGetValue(entityId, out var components) &&
               components.ContainsKey(typeof(T));
    }

    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        if (_components.TryGetValue(entityId, out var components))
        {
            components.Remove(typeof(T));
        }
    }

    public ECS.Interface.IQuery CreateQuery()
    {
        throw new NotImplementedException("MockWorld.CreateQuery 未实现");
    }

    public ECS.Interface.IArchetype GetArchetype(params Type[] componentTypes)
    {
        throw new NotImplementedException("MockWorld.GetArchetype 未实现");
    }

    public void Clear()
    {
        _components.Clear();
        _entityCount = 0;
    }

    #endregion
}