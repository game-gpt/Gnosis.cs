namespace Gnosis.Rendering.Backends.ShaderIR;

public abstract record ShaderIrType(string Name)
{
    public sealed record VoidType() : ShaderIrType("void");
    public sealed record BoolType() : ShaderIrType("bool");
    public sealed record IntType(int BitWidth = 32, bool Signed = true) : ShaderIrType(Signed ? $"i{BitWidth}" : $"u{BitWidth}");
    public sealed record FloatType(int BitWidth = 32) : ShaderIrType($"f{BitWidth}");
    public sealed record VectorType(ShaderIrType ElementType, int ComponentCount) : ShaderIrType($"vec{ComponentCount}<{ElementType.Name}>");
    public sealed record MatrixType(ShaderIrType ElementType, int ColumnCount, int RowCount) : ShaderIrType($"mat{ColumnCount}x{RowCount}<{ElementType.Name}>");
    public sealed record StructType(string StructName, IReadOnlyList<ShaderStructFieldIr> Fields) : ShaderIrType(StructName);
    public sealed record ImageType(ShaderIrType SampledType, int Dim, int Depth, bool Arrayed, bool Ms, int Format) : ShaderIrType("image");
    public sealed record SamplerType() : ShaderIrType("sampler");
    public sealed record SampledImageType(ShaderIrType Image) : ShaderIrType("sampled_image");
    public sealed record PointerType(ShaderIrType PointeeType, StorageClass Storage) : ShaderIrType($"ptr<{Storage},{PointeeType.Name}>");
    public sealed record FunctionType(ShaderIrType ReturnType, IReadOnlyList<ShaderIrType> ParameterTypes) : ShaderIrType("function");
    public sealed record AccelerationStructureType() : ShaderIrType("acceleration_structure");
    public sealed record ExternalType(string SymbolName) : ShaderIrType($"external<{SymbolName}>");
    public sealed record TensorType(ShaderIrType ElementType, IReadOnlyList<TensorDimensionIr> Dimensions) : ShaderIrType($"tensor<{ElementType.Name},[{string.Join(",", Dimensions.Select(d => d.IsDynamic ? "?" : d.StaticSize.ToString()))}]>");
}

public enum StorageClass
{
    UniformConstant = 0,
    Input = 1,
    Uniform = 2,
    Output = 3,
    Workgroup = 4,
    CrossWorkgroup = 5,
    Private = 6,
    Function = 7,
    Generic = 8,
    PushConstant = 9,
    AtomicCounter = 10,
    Image = 11,
    StorageBuffer = 12,
    RayPayloadKHR = 33,
    HitAttributeKHR = 34,
    IncomingRayPayloadKHR = 35,
    ShaderRecordBufferKHR = 36
}

public record ShaderStructFieldIr(string Name, ShaderIrType Type, uint Offset);

public sealed record TensorDimensionIr(bool IsDynamic, int StaticSize, string? DynamicName);
