using Gnosis.ECS.World;

namespace Gnosis.ECS.System;

/// <summary>
/// 系统接口，定义 ECS 系统的生命周期方法。
/// 系统通过 World 访问实体、组件和查询。
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

/// <summary>
/// 可访问 World 的系统接口。
/// 实现此接口的系统在注册时会被注入 World 引用。
/// </summary>
public interface IWorldSystem : ISystem
{
    /// <summary>
    /// 设置系统所属的 World
    /// </summary>
    void SetWorld(World.World world);
}
