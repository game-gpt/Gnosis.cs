using Acorn.Spirv.Data;

namespace SpirvCross.Net.Model;

/// <summary>
///     SPIR-V 类型系统模型，表示从 SPIR-V 指令中提取的类型信息。
/// </summary>
public abstract class SpirvType
{
    /// <summary>
    ///     类型结果 ID。
    /// </summary>
    public uint ResultId { get; init; }

    /// <summary>
    ///     类型名称（如果有 OpName 注解）。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
///     void 类型。
/// </summary>
public sealed class SpirvVoidType : SpirvType;

/// <summary>
///     布尔类型。
/// </summary>
public sealed class SpirvBoolType : SpirvType;

/// <summary>
///     整数类型。
/// </summary>
public sealed class SpirvIntType : SpirvType
{
    /// <summary>
    ///     位宽（8/16/32/64）。
    /// </summary>
    public uint BitWidth { get; init; }

    /// <summary>
    ///     是否有符号。
    /// </summary>
    public bool IsSigned { get; init; }
}

/// <summary>
///     浮点类型。
/// </summary>
public sealed class SpirvFloatType : SpirvType
{
    /// <summary>
    ///     位宽（16/32/64）。
    /// </summary>
    public uint BitWidth { get; init; }
}

/// <summary>
///     向量类型。
/// </summary>
public sealed class SpirvVectorType : SpirvType
{
    /// <summary>
    ///     分量类型 ID。
    /// </summary>
    public uint ComponentTypeId { get; init; }

    /// <summary>
    ///     分量数量（2/3/4）。
    /// </summary>
    public uint ComponentCount { get; init; }
}

/// <summary>
///     矩阵类型。
/// </summary>
public sealed class SpirvMatrixType : SpirvType
{
    /// <summary>
    ///     列类型 ID（向量类型）。
    /// </summary>
    public uint ColumnTypeId { get; init; }

    /// <summary>
    ///     列数。
    /// </summary>
    public uint ColumnCount { get; init; }

    /// <summary>
    ///     行数。
    /// </summary>
    public uint RowCount { get; init; }
}

/// <summary>
///     数组类型。
/// </summary>
public sealed class SpirvArrayType : SpirvType
{
    /// <summary>
    ///     元素类型 ID。
    /// </summary>
    public uint ElementTypeId { get; init; }

    /// <summary>
    ///     元素数量。
    /// </summary>
    public uint ElementCount { get; init; }
}

/// <summary>
///     结构体类型。
/// </summary>
public sealed class SpirvStructType : SpirvType
{
    /// <summary>
    ///     字段列表。
    /// </summary>
    public List<SpirvStructField> Fields { get; init; } = [];
}

/// <summary>
///     结构体字段。
/// </summary>
public sealed class SpirvStructField
{
    /// <summary>
    ///     字段类型 ID。
    /// </summary>
    public uint TypeId { get; init; }

    /// <summary>
    ///     字段名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     字段偏移（字节）。
    /// </summary>
    public uint Offset { get; init; }
}

/// <summary>
///     指针类型。
/// </summary>
public sealed class SpirvPointerType : SpirvType
{
    /// <summary>
    ///     指向的类型 ID。
    /// </summary>
    public uint PointeeTypeId { get; init; }

    /// <summary>
    ///     存储类。
    /// </summary>
    public SpirvStorageClass StorageClass { get; init; }
}

/// <summary>
///     函数类型。
/// </summary>
public sealed class SpirvFunctionType : SpirvType
{
    /// <summary>
    ///     返回类型 ID。
    /// </summary>
    public uint ReturnTypeId { get; init; }

    /// <summary>
    ///     参数类型 ID 列表。
    /// </summary>
    public List<uint> ParameterTypeIds { get; init; } = [];
}

/// <summary>
///     图像类型。
/// </summary>
public sealed class SpirvImageType : SpirvType
{
    /// <summary>
    ///     采样类型 ID。
    /// </summary>
    public uint SampledTypeId { get; init; }

    /// <summary>
    ///     图像维度。
    /// </summary>
    public SpirvImageDim Dim { get; init; }

    /// <summary>
    ///     是否深度图像。
    /// </summary>
    public bool IsDepth { get; init; }

    /// <summary>
    ///     是否数组图像。
    /// </summary>
    public bool IsArrayed { get; init; }

    /// <summary>
    ///     是否多重采样。
    /// </summary>
    public bool IsMultisampled { get; init; }

    /// <summary>
    ///     是否采样图像。
    /// </summary>
    public uint Sampled { get; init; }
}

/// <summary>
///     采样器类型。
/// </summary>
public sealed class SpirvSamplerType : SpirvType;

/// <summary>
///     采样图像类型。
/// </summary>
public sealed class SpirvSampledImageType : SpirvType
{
    /// <summary>
    ///     图像类型 ID。
    /// </summary>
    public uint ImageTypeId { get; init; }
}
