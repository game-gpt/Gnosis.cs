namespace Gnosis.Core.Lifecycle;

/// <summary>
/// 系统基础接口，定义初始化和关闭生命周期。
/// 上层模块（如 Gnosis.ECS）可扩展此接口添加更多生命周期方法。
/// </summary>
public interface ISystem
{
    /// <summary>
    /// 系统初始化
    /// </summary>
    void Initialize();

    /// <summary>
    /// 系统关闭
    /// </summary>
    void Shutdown();
}
