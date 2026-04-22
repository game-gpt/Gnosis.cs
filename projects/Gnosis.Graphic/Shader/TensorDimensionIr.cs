using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 张量维度 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class TensorDimensionIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public int Size { get; set; }

    public int Stride { get; set; }

    #endregion
}
