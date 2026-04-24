namespace Gnosis.IR.Shader;

public sealed class ShaderGlobalVariableIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; } = ShaderIrType.Void;

    public StorageClass Storage { get; set; }

    public uint ResultId { get; set; }

    public ShaderResourceIr? Resource { get; set; }

    public uint? Location { get; set; }

    public string? Builtin { get; set; }

    #endregion
}
