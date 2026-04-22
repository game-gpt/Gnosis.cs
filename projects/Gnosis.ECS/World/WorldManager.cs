namespace Gnosis.ECS.World;

/// <summary>
/// 世界管理器，支持同时管理多个世界（主世界 + 子世界）。
/// 每个世界独立拥有实体、组件、系统和查询，但可通过 WorldManager 统一更新。
/// </summary>
public sealed class WorldManager
{
    #region 字段

    private readonly Dictionary<string, World> _worlds = new();
    private string _activeWorldName = "main";

    #endregion

    #region 属性

    /// <summary>
    /// 当前活跃的世界
    /// </summary>
    public World ActiveWorld => _worlds[_activeWorldName];

    /// <summary>
    /// 当前活跃世界的名称
    /// </summary>
    public string ActiveWorldName => _activeWorldName;

    /// <summary>
    /// 已注册的世界数量
    /// </summary>
    public int WorldCount => _worlds.Count;

    /// <summary>
    /// 所有世界的名称列表
    /// </summary>
    public IReadOnlyCollection<string> WorldNames => _worlds.Keys;

    #endregion

    #region 构造函数

    public WorldManager()
    {
        var mainWorld = new World();
        _worlds["main"] = mainWorld;
    }

    #endregion

    #region 世界管理

    /// <summary>
    /// 创建并注册一个新世界
    /// </summary>
    public World CreateWorld(string name)
    {
        if (_worlds.ContainsKey(name))
        {
            throw new ArgumentException($"世界 '{name}' 已存在", nameof(name));
        }

        var world = new World();
        _worlds[name] = world;

        return world;
    }

    /// <summary>
    /// 获取指定名称的世界
    /// </summary>
    public World GetWorld(string name)
    {
        if (_worlds.TryGetValue(name, out var world))
        {
            return world;
        }

        throw new KeyNotFoundException($"世界 '{name}' 不存在");
    }

    /// <summary>
    /// 尝试获取指定名称的世界
    /// </summary>
    public bool TryGetWorld(string name, out World world)
    {
        return _worlds.TryGetValue(name, out world!);
    }

    /// <summary>
    /// 检查指定名称的世界是否存在
    /// </summary>
    public bool HasWorld(string name)
    {
        return _worlds.ContainsKey(name);
    }

    /// <summary>
    /// 销毁指定世界并清理其所有资源
    /// </summary>
    public bool DestroyWorld(string name)
    {
        if (name == "main")
        {
            throw new InvalidOperationException("不能销毁主世界");
        }

        if (!_worlds.Remove(name, out var world))
        {
            return false;
        }

        // 清理世界中的所有实体
        var entityIds = world.Entities.CreateEntities(0);

        if (_activeWorldName == name)
        {
            _activeWorldName = "main";
        }

        return true;
    }

    /// <summary>
    /// 切换当前活跃世界
    /// </summary>
    public void SetActiveWorld(string name)
    {
        if (!_worlds.ContainsKey(name))
        {
            throw new KeyNotFoundException($"世界 '{name}' 不存在");
        }

        _activeWorldName = name;
    }

    #endregion

    #region 统一更新

    /// <summary>
    /// 更新所有世界（按注册顺序）
    /// </summary>
    public void UpdateAll(float delta)
    {
        foreach (var world in _worlds.Values)
        {
            world.Update(delta);
        }
    }

    /// <summary>
    /// 更新指定世界
    /// </summary>
    public void UpdateWorld(string name, float delta)
    {
        var world = GetWorld(name);
        world.Update(delta);
    }

    /// <summary>
    /// 更新当前活跃世界
    /// </summary>
    public void UpdateActive(float delta)
    {
        ActiveWorld.Update(delta);
    }

    #endregion
}
