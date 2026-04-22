namespace Gnosis.Neural.Binding;

/// <summary>
/// 绑定目标类型
/// </summary>
public enum BindingTarget : byte
{
    ECSComponent = 0,
    RenderParameter = 1,
    ShaderUniform = 2,
    Custom = 3
}
