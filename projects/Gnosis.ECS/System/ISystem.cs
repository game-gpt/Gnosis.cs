using Gnosis.ECS.World;

namespace Gnosis.ECS.System;

public interface ISystem : Gnosis.Core.ISystem
{
    /// <summary>
    /// 系统执行阶段
    /// </summary>
    SystemPhase Phase { get; }

    /// <summary>
    /// 系统帧更新
    /// </summary>
    void Update(float delta);
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
