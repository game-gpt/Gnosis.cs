namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 执行模型枚举
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
