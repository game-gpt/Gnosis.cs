namespace Gnosis.Neural.Binding;

/// <summary>
/// 张量与 ECS 组件绑定接口，将神经网络输出映射到游戏逻辑
/// </summary>
public interface ITensorBinding
{
    /// <summary>
    /// 绑定名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 绑定目标类型
    /// </summary>
    BindingTarget Target { get; }

    /// <summary>
    /// 源张量名称
    /// </summary>
    string SourceTensorName { get; }

    /// <summary>
    /// 目标路径（组件字段路径或渲染参数名）
    /// </summary>
    string TargetPath { get; }

    /// <summary>
    /// 应用绑定，将张量数据写入目标
    /// </summary>
    void Apply(object target, float[] data);
}
