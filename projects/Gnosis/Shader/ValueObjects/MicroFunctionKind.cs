namespace Gnosis.Shader.ValueObjects;

public enum MicroFunctionKind
{
    Vertex = 0,
    Fragment = 1,
    Geometry = 2,
    TessControl = 3,
    TessEvaluation = 4,
    Compute = 5,
    RayGen = 6,
    RayAnyHit = 7,
    RayClosestHit = 8,
    RayMiss = 9,
    RayIntersection = 10,
    Neural = 11,
    Diffusion = 12
}
