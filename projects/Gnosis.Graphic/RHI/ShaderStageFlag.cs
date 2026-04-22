namespace Gnosis.Graphic.RHI;

/// <summary>
/// 着色器阶段标志位
/// </summary>
[Flags]
public enum ShaderStageFlag
{
    Vertex = 1,
    TessControl = 2,
    TessEvaluation = 4,
    Geometry = 8,
    Fragment = 16,
    Compute = 32,
    AllGraphics = Vertex | TessControl | TessEvaluation | Geometry | Fragment,
    All = AllGraphics | Compute
}
