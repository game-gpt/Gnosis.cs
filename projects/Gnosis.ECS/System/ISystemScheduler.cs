namespace Gnosis.ECS.System;

/// <summary>
/// 系统调度器接口，负责系统的注册、调度和执行
/// </summary>
public interface ISystemScheduler
{
    /// <summary>
    /// 注册系统
    /// </summary>
    void RegisterSystem(ISystem system);

    /// <summary>
    /// 注销系统
    /// </summary>
    void UnregisterSystem(ISystem system);

    /// <summary>
    /// 更新所有活跃系统
    /// </summary>
    void Update(float delta);

    /// <summary>
    /// 启用系统
    /// </summary>
    void EnableSystem(ISystem system);

    /// <summary>
    /// 禁用系统
    /// </summary>
    void DisableSystem(ISystem system);
}
