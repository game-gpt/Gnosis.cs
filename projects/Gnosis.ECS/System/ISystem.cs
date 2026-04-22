namespace Gnosis.ECS.System;

/// <summary>
/// 系统接口，定义 ECS 系统的生命周期方法
/// </summary>
public interface ISystem
{
    /// <summary>
    /// 系统执行阶段
    /// </summary>
    SystemPhase Phase { get; }

    /// <summary>
    /// 系统初始化
    /// </summary>
    void Initialize();

    /// <summary>
    /// 系统帧更新
    /// </summary>
    void Update(float delta);

    /// <summary>
    /// 系统关闭
    /// </summary>
    void Shutdown();
}
