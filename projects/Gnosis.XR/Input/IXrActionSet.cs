namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作集接口，管理一组相关的 XR 动作
/// </summary>
public interface IXrActionSet
{
    /// <summary>
    /// 动作集名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 优先级（数值越高优先级越高）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 动作列表
    /// </summary>
    IReadOnlyList<IXrAction> Actions { get; }

    /// <summary>
    /// 获取指定名称的动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <returns>动作实例，未找到返回 null</returns>
    IXrAction? GetAction(string name);

    /// <summary>
    /// 创建并添加一个新动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <param name="actionType">动作类型</param>
    /// <returns>新创建的动作</returns>
    IXrAction CreateAction(string name, XrActionType actionType);

    /// <summary>
    /// 移除指定名称的动作
    /// </summary>
    /// <param name="name">动作名称</param>
    void RemoveAction(string name);

    /// <summary>
    /// 同步动作状态（每帧调用，从 XR 运行时获取最新输入数据）
    /// </summary>
    void Sync();
}
