using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 实体查询接口，支持链式调用构建查询条件
/// </summary>
public interface IQuery
{
    /// <summary>
    /// 查询必须包含所有指定组件类型的实体
    /// </summary>
    IQuery All<T>() where T : struct;

    /// <summary>
    /// 查询包含任意指定组件类型的实体
    /// </summary>
    IQuery Any<T>() where T : struct;

    /// <summary>
    /// 排除包含指定组件类型的实体
    /// </summary>
    IQuery None<T>() where T : struct;

    /// <summary>
    /// 只返回自上次查询以来指定组件发生变更的实体
    /// </summary>
    IQuery Changed<T>() where T : struct;

    /// <summary>
    /// 执行查询并返回匹配的实体 ID 集合
    /// </summary>
    IEnumerable<EntityId> Build();

    /// <summary>
    /// 创建查询迭代器，支持组件数据的批量遍历
    /// </summary>
    QueryIterator Iterate();
}
