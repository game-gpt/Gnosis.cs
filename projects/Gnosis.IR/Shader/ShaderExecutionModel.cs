namespace Gnosis.IR.Shader;

public enum ShaderExecutionModel
{
    Vertex,
    TessellationControl,
    TessellationEvaluation,
    Geometry,
    Fragment,
    GLCompute,
    Kernel,
    RayGenerationKHR,
    IntersectionKHR,
    AnyHitKHR,
    ClosestHitKHR,
    MissKHR,
    CallableKHR,
    Mesh,
    Task,
    Compute = GLCompute,
    RayGeneration = RayGenerationKHR,
    Intersection = IntersectionKHR,
    AnyHit = AnyHitKHR,
    ClosestHit = ClosestHitKHR,
    Miss = MissKHR,
    Callable = CallableKHR
}
