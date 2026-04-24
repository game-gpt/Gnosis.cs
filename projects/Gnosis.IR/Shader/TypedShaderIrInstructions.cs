namespace Gnosis.IR.Shader;

public abstract class TypedShaderIrInstruction : ShaderIrInstruction
{
    public uint ResultId { get; set; }
}

public sealed class LabelInstruction : ShaderIrInstruction
{
    public uint LabelId { get; set; }
}

public sealed class ArithmeticInstruction : TypedShaderIrInstruction
{
    public ShaderIrOpCode OpCode { get; set; }
    public uint LeftId { get; set; }
    public uint RightId { get; set; }
}

public sealed class CompareInstruction : TypedShaderIrInstruction
{
    public ShaderIrOpCode OpCode { get; set; }
    public uint LeftId { get; set; }
    public uint RightId { get; set; }
}

public sealed class LogicalInstruction : TypedShaderIrInstruction
{
    public ShaderIrOpCode OpCode { get; set; }
    public uint LeftId { get; set; }
    public uint RightId { get; set; }
}

public sealed class VectorSwizzleInstruction : TypedShaderIrInstruction
{
    public uint VectorId { get; set; }
    public int[] Components { get; set; } = [];
}

public sealed class VectorConstructInstruction : TypedShaderIrInstruction
{
    public uint[] ComponentIds { get; set; } = [];
}

public sealed class VectorBuiltinInstruction : TypedShaderIrInstruction
{
    public ShaderIrOpCode OpCode { get; set; }
    public uint[] OperandIds { get; set; } = [];
}

public sealed class MatrixInstruction : TypedShaderIrInstruction
{
    public ShaderIrOpCode OpCode { get; set; }
    public uint LeftId { get; set; }
    public uint RightId { get; set; }
}

public sealed class TextureSampleInstruction : TypedShaderIrInstruction
{
    public uint SampledImageId { get; set; }
    public uint CoordinateId { get; set; }
}

public sealed class TextureLoadInstruction : TypedShaderIrInstruction
{
    public uint ImageId { get; set; }
    public uint CoordinateId { get; set; }
}

public sealed class TextureStoreInstruction : ShaderIrInstruction
{
    public uint ImageId { get; set; }
    public uint ValueId { get; set; }
}

public sealed class BranchInstruction : ShaderIrInstruction
{
    public uint TargetLabelId { get; set; }
}

public sealed class BranchConditionalInstruction : ShaderIrInstruction
{
    public uint ConditionId { get; set; }
    public uint TrueLabelId { get; set; }
    public uint FalseLabelId { get; set; }
}

public sealed class ReturnInstruction : ShaderIrInstruction
{
    public uint? ValueId { get; set; }
}

public sealed class DiscardInstruction : ShaderIrInstruction { }

public sealed class SelectionMergeInstruction : ShaderIrInstruction
{
    public uint MergeLabelId { get; set; }
}

public sealed class LoopMergeInstruction : ShaderIrInstruction
{
    public uint MergeLabelId { get; set; }
    public uint ContinueLabelId { get; set; }
}

public sealed class LoadInstruction : TypedShaderIrInstruction
{
    public uint PointerId { get; set; }
    public uint? Value { get; set; }
}

public sealed class StoreInstruction : ShaderIrInstruction
{
    public uint PointerId { get; set; }
    public uint ValueId { get; set; }
}

public sealed class AccessChainInstruction : TypedShaderIrInstruction
{
    public uint BaseId { get; set; }
    public uint[] IndexIds { get; set; } = [];
}

public sealed class CompositeConstructInstruction : TypedShaderIrInstruction
{
    public uint[] ConstituentIds { get; set; } = [];
}

public sealed class CompositeExtractInstruction : TypedShaderIrInstruction
{
    public uint CompositeId { get; set; }
    public int[] Indices { get; set; } = [];
}

public sealed class CallInstruction : TypedShaderIrInstruction
{
    public string FunctionName { get; set; } = string.Empty;
    public uint[] ArgumentIds { get; set; } = [];
}

public sealed class CallBuiltinInstruction : TypedShaderIrInstruction
{
    public string BuiltinName { get; set; } = string.Empty;
    public uint[] ArgumentIds { get; set; } = [];
}

public sealed class ConvertInstruction : TypedShaderIrInstruction
{
    public uint ValueId { get; set; }
}

public sealed class PhiInstruction : TypedShaderIrInstruction { }
