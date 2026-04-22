using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 执行模型枚举（已迁移至 Gnosis.IR.Shader）
/// </summary>
public enum ShaderExecutionModel
{
    Vertex,
    Fragment,
    Compute,
    Geometry,
    TessellationControl,
    TessellationEvaluation,
    RayGeneration,
    Intersection,
    AnyHit,
    ClosestHit,
    Miss,
    Callable,
    Mesh,
    Task
}
