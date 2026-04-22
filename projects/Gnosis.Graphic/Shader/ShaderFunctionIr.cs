namespace Gnosis.Graphic.Shader;

public enum ShaderExecutionModel
{
    Vertex,
    Fragment,
    GLCompute,
    RayGenerationKHR,
    ClosestHitKHR,
    MissKHR,
    AnyHitKHR,
    IntersectionKHR
}

public sealed record ShaderFunctionIr(
    string Name,
    ShaderExecutionModel? ExecutionModel,
    IReadOnlyList<ShaderIrParameter> Parameters,
    ShaderIrType ReturnType,
    IReadOnlyList<ShaderIrInstruction> Instructions,
    IReadOnlyList<LocalVariableInstruction> LocalVariables,
    IReadOnlyList<ShaderAttributeIr> Attributes,
    bool IsEntryPoint)
{
    public uint ResultId { get; set; }
}

public sealed record ShaderIrParameter(
    string Name,
    ShaderIrType Type,
    StorageClass Storage = StorageClass.Function,
    uint? Location = null,
    string? Builtin = null)
{
    public uint ResultId { get; set; }
}
