namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 资源 IR 定义
/// </summary>
public sealed class ShaderResourceIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderResourceKind Kind { get; set; }

    public ShaderIrType Type { get; set; } = ShaderIrType.Void;

    public uint DescriptorSet { get; set; }

    public uint Binding { get; set; }

    #endregion
}
