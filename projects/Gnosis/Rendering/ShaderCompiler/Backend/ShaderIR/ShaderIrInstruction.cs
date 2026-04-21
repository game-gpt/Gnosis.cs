namespace Gnosis.Rendering.ShaderCompiler.Backend.ShaderIR;

public enum ShaderIrOpCode
{
    Nop,

    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Negate,

    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessEqual,
    GreaterEqual,

    LogicalAnd,
    LogicalOr,
    LogicalNot,

    VectorSwizzle,
    VectorConstruct,
    Dot,
    Cross,
    Normalize,
    Length,

    MatrixMultiply,
    MatrixTranspose,
    MatrixInverse,

    TextureSample,
    TextureLoad,
    TextureStore,

    Branch,
    BranchConditional,
    Return,
    Discard,
    LoopMerge,
    SelectionMerge,

    Load,
    Store,
    AccessChain,
    CompositeConstruct,
    CompositeExtract,

    Call,
    CallBuiltin,

    Convert,

    Phi
}

public abstract record ShaderIrInstruction(ShaderIrOpCode OpCode)
{
    public ShaderIrType? ResultType { get; init; }
    public uint ResultId { get; set; }
}

public sealed record ArithmeticInstruction(
    ShaderIrOpCode OpCode,
    ShaderIrType ResultType,
    uint LeftId,
    uint RightId) : ShaderIrInstruction(OpCode)
{
    public ArithmeticInstruction Negate(ShaderIrType type, uint operandId) =>
        new(ShaderIrOpCode.Negate, type, operandId, 0);
}

public sealed record CompareInstruction(
    ShaderIrOpCode OpCode,
    ShaderIrType ResultType,
    uint LeftId,
    uint RightId) : ShaderIrInstruction(OpCode);

public sealed record LogicalInstruction(
    ShaderIrOpCode OpCode,
    ShaderIrType ResultType,
    uint LeftId,
    uint RightId) : ShaderIrInstruction(OpCode)
{
    public LogicalInstruction Not(ShaderIrType type, uint operandId) =>
        new(ShaderIrOpCode.LogicalNot, type, operandId, 0);
}

public sealed record VectorSwizzleInstruction(
    ShaderIrType ResultType,
    uint VectorId,
    int[] Components) : ShaderIrInstruction(ShaderIrOpCode.VectorSwizzle);

public sealed record VectorConstructInstruction(
    ShaderIrType ResultType,
    uint[] ComponentIds) : ShaderIrInstruction(ShaderIrOpCode.VectorConstruct);

public sealed record VectorBuiltinInstruction(
    ShaderIrOpCode OpCode,
    ShaderIrType ResultType,
    uint[] OperandIds) : ShaderIrInstruction(OpCode);

public sealed record MatrixInstruction(
    ShaderIrOpCode OpCode,
    ShaderIrType ResultType,
    uint LeftId,
    uint RightId) : ShaderIrInstruction(OpCode);

public sealed record TextureSampleInstruction(
    ShaderIrType ResultType,
    uint SampledImageId,
    uint CoordinateId,
    uint? LodId = null) : ShaderIrInstruction(ShaderIrOpCode.TextureSample);

public sealed record TextureLoadInstruction(
    ShaderIrType ResultType,
    uint ImageId,
    uint CoordinateId) : ShaderIrInstruction(ShaderIrOpCode.TextureLoad);

public sealed record TextureStoreInstruction(
    uint ImageId,
    uint CoordinateId,
    uint ValueId) : ShaderIrInstruction(ShaderIrOpCode.TextureStore);

public sealed record BranchInstruction(uint TargetLabelId) : ShaderIrInstruction(ShaderIrOpCode.Branch);

public sealed record BranchConditionalInstruction(
    uint ConditionId,
    uint TrueLabelId,
    uint FalseLabelId) : ShaderIrInstruction(ShaderIrOpCode.BranchConditional);

public sealed record ReturnInstruction(uint? ValueId = null) : ShaderIrInstruction(ShaderIrOpCode.Return);

public sealed record DiscardInstruction() : ShaderIrInstruction(ShaderIrOpCode.Discard);

public sealed record LoopMergeInstruction(
    uint MergeLabelId,
    uint ContinueLabelId) : ShaderIrInstruction(ShaderIrOpCode.LoopMerge);

public sealed record SelectionMergeInstruction(
    uint MergeLabelId) : ShaderIrInstruction(ShaderIrOpCode.SelectionMerge);

public sealed record LoadInstruction(
    ShaderIrType ResultType,
    uint PointerId,
    uint? Value = null) : ShaderIrInstruction(ShaderIrOpCode.Load);

public sealed record StoreInstruction(
    uint PointerId,
    uint ValueId) : ShaderIrInstruction(ShaderIrOpCode.Store);

public sealed record AccessChainInstruction(
    ShaderIrType ResultType,
    uint BaseId,
    uint[] IndexIds) : ShaderIrInstruction(ShaderIrOpCode.AccessChain);

public sealed record CompositeConstructInstruction(
    ShaderIrType ResultType,
    uint[] ConstituentIds) : ShaderIrInstruction(ShaderIrOpCode.CompositeConstruct);

public sealed record CompositeExtractInstruction(
    ShaderIrType ResultType,
    uint CompositeId,
    int[] Indices) : ShaderIrInstruction(ShaderIrOpCode.CompositeExtract);

public sealed record CallInstruction(
    ShaderIrType ResultType,
    string FunctionName,
    uint[] ArgumentIds) : ShaderIrInstruction(ShaderIrOpCode.Call);

public sealed record CallBuiltinInstruction(
    ShaderIrType ResultType,
    string BuiltinName,
    uint[] ArgumentIds) : ShaderIrInstruction(ShaderIrOpCode.CallBuiltin);

public sealed record ConvertInstruction(
    ShaderIrType ResultType,
    uint ValueId) : ShaderIrInstruction(ShaderIrOpCode.Convert);

public sealed record PhiInstruction(
    ShaderIrType ResultType,
    (uint ValueId, uint LabelId)[] Incoming) : ShaderIrInstruction(ShaderIrOpCode.Phi);

public sealed record LabelInstruction(uint LabelId) : ShaderIrInstruction(ShaderIrOpCode.Nop);

public sealed record LocalVariableInstruction(
    ShaderIrType ResultType,
    string Name,
    uint InitializerId = 0) : ShaderIrInstruction(ShaderIrOpCode.Nop);
