namespace Gnosis.Widget.Scene;

public sealed class SceneData
{
    public string Name { get; set; } = "Untitled";

    public string Path { get; set; } = "";

    public bool IsDirty { get; set; }

    public IReadOnlyList<EntityData> Entities => _entities;

    private readonly List<EntityData> _entities = [];

    public void AddEntity(EntityData entity)
    {
        _entities.Add(entity);
        IsDirty = true;
    }

    public void RemoveEntity(EntityData entity)
    {
        _entities.Remove(entity);
        IsDirty = true;
    }

    public EntityData? FindEntity(string id)
    {
        return _entities.FirstOrDefault(e => e.Id == id);
    }

    public void Clear()
    {
        _entities.Clear();
        IsDirty = true;
    }
}

public sealed class EntityData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "Entity";

    public string? ParentId { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float RotationX { get; set; }

    public float RotationY { get; set; }

    public float RotationZ { get; set; }

    public float ScaleX { get; set; } = 1;

    public float ScaleY { get; set; } = 1;

    public float ScaleZ { get; set; } = 1;

    public IReadOnlyList<ComponentData> Components => _components;

    private readonly List<ComponentData> _components = [];

    public void AddComponent(ComponentData component)
    {
        _components.Add(component);
    }

    public void RemoveComponent(ComponentData component)
    {
        _components.Remove(component);
    }

    public T? GetComponent<T>() where T : ComponentData
    {
        return _components.OfType<T>().FirstOrDefault();
    }
}

public abstract class ComponentData
{
    public string TypeName { get; }

    protected ComponentData(string typeName)
    {
        TypeName = typeName;
    }
}

public sealed class SceneService
{
    #region 字段

    private SceneData? _currentScene;

    #endregion

    #region 属性

    public SceneData? CurrentScene => _currentScene;

    public bool HasUnsavedChanges => _currentScene?.IsDirty ?? false;

    #endregion

    #region 事件

    public event Action<SceneData>? SceneOpened;
    public event Action<SceneData>? SceneSaved;
    public event Action? SceneClosed;

    #endregion

    #region 公开方法

    public SceneData NewScene(string name = "Untitled")
    {
        _currentScene = new SceneData { Name = name };
        SceneOpened?.Invoke(_currentScene);
        return _currentScene;
    }

    public void OpenScene(SceneData scene)
    {
        _currentScene = scene;
        SceneOpened?.Invoke(scene);
    }

    public void SaveScene()
    {
        if (_currentScene == null)
        {
            return;
        }

        _currentScene.IsDirty = false;
        SceneSaved?.Invoke(_currentScene);
    }

    public void CloseScene()
    {
        _currentScene = null;
        SceneClosed?.Invoke();
    }

    public EntityData CreateEntity(string name)
    {
        var entity = new EntityData { Name = name };

        if (_currentScene != null)
        {
            _currentScene.AddEntity(entity);
        }

        return entity;
    }

    public void DeleteEntity(EntityData entity)
    {
        _currentScene?.RemoveEntity(entity);
    }

    #endregion
}
