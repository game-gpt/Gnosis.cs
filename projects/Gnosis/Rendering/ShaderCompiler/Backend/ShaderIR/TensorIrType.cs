using Gnosis.Rendering.ShaderCompiler.Backend.ShaderIR;

namespace Gnosis.Rendering.ShaderCompiler.Backend.ShaderIR;

/// <summary>
/// 张量类型 IR，表示 tensor&lt;T, [dims]&gt; 的类型信息
/// </summary>
public sealed record TensorIrType(
    string Name,
    ShaderIrType ElementType,
    IReadOnlyList<TensorDimensionIr> Dimensions) : ShaderIrType(Name);

/// <summary>
/// 张量维度 IR
/// </summary>
public sealed record TensorDimensionIr(
    bool IsDynamic,
    int StaticSize,
    string? DynamicName);
