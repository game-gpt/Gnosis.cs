namespace Gnosis.ECS.System;

/// <summary>
/// 系统组，管理一组相关的系统
/// </summary>
public sealed class SystemGroup : ISystemGroup
{
    #region 字段

    private readonly List<ISystem> _systems = [];

    #endregion

    #region 属性

    public string Name { get; }
    public IReadOnlyList<ISystem> Systems => _systems;

    #endregion

    #region 构造函数

    public SystemGroup(string name)
    {
        Name = name;
    }

    #endregion

    #region 公开方法

    public void AddSystem(ISystem system) => _systems.Add(system);

    public void RemoveSystem(ISystem system) => _systems.Remove(system);

    public void Update(float delta)
    {
        foreach (var system in _systems)
        {
            system.Update(delta);
        }
    }

    #endregion
}
