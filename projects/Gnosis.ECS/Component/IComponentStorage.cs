using Gnosis.ECS.Archetype;

namespace Gnosis.ECS.Component;

/// <summary>
/// 组件存储接口，管理组件池和 Archetype 查询
/// </summary>
public interface IComponentStorage
{
    /// <summary>
    /// 获取或创建指定类型的组件池
    /// </summary>
    IComponentPool GetPool<T>() where T : struct;

    /// <summary>
    /// 获取匹配指定组件组合的 Archetype
    /// </summary>
    IArchetype GetArchetypeStorage(params Type[] componentTypes);
}
