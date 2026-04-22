using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 结构体字段 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderStructFieldIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; }

    public uint Offset { get; set; }

    public uint? MatrixStride { get; set; }

    public bool IsRowMajor { get; set; }

    #endregion
}
