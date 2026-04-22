using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 资源 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderResourceIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderResourceKind Kind { get; set; }

    public ShaderIrType Type { get; set; }

    public uint DescriptorSet { get; set; }

    public uint Binding { get; set; }

    #endregion
}
