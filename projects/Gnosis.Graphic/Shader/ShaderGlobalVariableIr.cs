using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 全局变量 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderGlobalVariableIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; }

    public StorageClass StorageClass { get; set; }

    public ShaderResourceKind? ResourceKind { get; set; }

    public uint? DescriptorSet { get; set; }

    public uint? Binding { get; set; }

    public uint? Location { get; set; }

    public uint? BuiltIn { get; set; }

    #endregion
}
