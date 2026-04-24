namespace Gnosis.VM;

/// <summary>
///     游戏世界接口，抽象 ECS 世界操作
/// </summary>
public interface IGameWorld
{
    /// <summary>
    ///     创建实体
    /// </summary>
    /// <returns>实体 ID</returns>
    long SpawnEntity();

    /// <summary>
    ///     销毁实体
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    void DestroyEntity(long entityId);

    /// <summary>
    ///     向实体添加组件
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="componentType">组件类型名称</param>
    /// <param name="fields">组件字段</param>
    void AddComponent(long entityId, string componentType, Dictionary<string, object?> fields);

    /// <summary>
    ///     获取组件字段值
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="componentType">组件类型名称</param>
    /// <param name="fieldName">字段名称</param>
    /// <returns>字段值</returns>
    object? GetComponent(long entityId, string componentType, string fieldName);

    /// <summary>
    ///     设置组件字段值
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="componentType">组件类型名称</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="value">字段值</param>
    void SetComponent(long entityId, string componentType, string fieldName, object? value);

    /// <summary>
    ///     移除组件
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="componentType">组件类型名称</param>
    void RemoveComponent(long entityId, string componentType);

    /// <summary>
    ///     检查实体是否拥有某组件
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="componentType">组件类型名称</param>
    /// <returns>是否拥有该组件</returns>
    bool HasComponent(long entityId, string componentType);

    /// <summary>
    ///     查询实体
    /// </summary>
    /// <param name="all">必须拥有的组件</param>
    /// <param name="any">至少拥有一个的组件</param>
    /// <param name="none">不能拥有的组件</param>
    /// <returns>匹配的实体 ID 列表</returns>
    IReadOnlyList<long> QueryEntities(IReadOnlyList<string> all, IReadOnlyList<string> any, IReadOnlyList<string> none);

    /// <summary>
    ///     执行一帧更新
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（毫秒）</param>
    void Update(double deltaTime);
}
