namespace Gnosis.ECS.System;

/// <summary>
/// 系统组接口，管理一组相关的系统
/// </summary>
public interface ISystemGroup
{
    /// <summary>
    /// 系统组名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 系统组中的系统列表
    /// </summary>
    IReadOnlyList<ISystem> Systems { get; }

    /// <summary>
    /// 添加系统到组
    /// </summary>
    void AddSystem(ISystem system);

    /// <summary>
    /// 从组中移除系统
    /// </summary>
    void RemoveSystem(ISystem system);

    /// <summary>
    /// 更新组内所有系统
    /// </summary>
    void Update(float delta);
}
