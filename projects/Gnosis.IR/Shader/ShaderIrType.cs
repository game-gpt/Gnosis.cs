namespace Gnosis.IR.Shader;

public abstract class ShaderIrType
{
    #region Void

    public sealed class VoidType : ShaderIrType
    {
        public override string ToString() => "void";
    }

    #endregion

    #region Bool

    public sealed class BoolType : ShaderIrType
    {
        public override string ToString() => "bool";
    }

    #endregion

    #region Int

    public sealed class IntType : ShaderIrType
    {
        public uint BitWidth { get; }
        public bool Signed { get; }

        public IntType(uint bitWidth = 32, bool signed = true)
        {
            BitWidth = bitWidth;
            Signed = signed;
        }

        public override string ToString() => Signed ? $"i{BitWidth}" : $"u{BitWidth}";
    }

    #endregion

    #region Float

    public sealed class FloatType : ShaderIrType
    {
        public uint BitWidth { get; }

        public FloatType(uint bitWidth = 32)
        {
            BitWidth = bitWidth;
        }

        public override string ToString() => $"f{BitWidth}";
    }

    #endregion

    #region Vector

    public sealed class VectorType : ShaderIrType
    {
        public ShaderIrType ElementType { get; }
        public int ComponentCount { get; }

        public VectorType(ShaderIrType elementType, int componentCount)
        {
            ElementType = elementType;
            ComponentCount = componentCount;
        }

        public override string ToString() => $"vec{ComponentCount}<{ElementType}>";
    }

    #endregion

    #region Matrix

    public sealed class MatrixType : ShaderIrType
    {
        public ShaderIrType ElementType { get; }
        public int RowCount { get; }
        public int ColumnCount { get; }

        public MatrixType(ShaderIrType elementType, int rowCount, int columnCount)
        {
            ElementType = elementType;
            RowCount = rowCount;
            ColumnCount = columnCount;
        }

        public override string ToString() => $"mat{RowCount}x{ColumnCount}<{ElementType}>";
    }

    #endregion

    #region Array

    public sealed class ArrayType : ShaderIrType
    {
        public ShaderIrType ElementType { get; }
        public int Length { get; }

        public ArrayType(ShaderIrType elementType, int length)
        {
            ElementType = elementType;
            Length = length;
        }

        public override string ToString() => $"[{ElementType}; {Length}]";
    }

    #endregion

    #region Struct

    public sealed class StructType : ShaderIrType
    {
        public string Name { get; }
        public List<ShaderStructFieldIr> Fields { get; }

        public StructType(string name, List<ShaderStructFieldIr> fields)
        {
            Name = name;
            Fields = fields;
        }

        public override string ToString() => Name;
    }

    #endregion

    #region Pointer

    public sealed class PointerType : ShaderIrType
    {
        public ShaderIrType PointeeType { get; }
        public StorageClass Storage { get; }

        public PointerType(ShaderIrType pointeeType, StorageClass storage)
        {
            PointeeType = pointeeType;
            Storage = storage;
        }

        public override string ToString() => $"*{PointeeType}";
    }

    #endregion

    #region Function

    public sealed class FunctionType : ShaderIrType
    {
        public ShaderIrType ReturnType { get; }
        public List<ShaderIrType> ParameterTypes { get; }

        public FunctionType(ShaderIrType returnType, List<ShaderIrType> parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public override string ToString() => $"({ReturnType})({string.Join(", ", ParameterTypes)})";
    }

    #endregion

    #region Sampler

    public sealed class SamplerType : ShaderIrType
    {
        public override string ToString() => "sampler";
    }

    #endregion

    #region Image

    public sealed class ImageType : ShaderIrType
    {
        public ShaderIrType SampledType { get; }
        public uint Dim { get; }
        public uint Depth { get; }
        public uint Arrayed { get; }
        public uint Ms { get; }
        public uint Sampled { get; }
        public uint ImageFormat { get; }

        public ImageType(ShaderIrType sampledType, uint dim, uint depth = 2, uint arrayed = 0, uint ms = 0, uint sampled = 1, uint imageFormat = 0)
        {
            SampledType = sampledType;
            Dim = dim;
            Depth = depth;
            Arrayed = arrayed;
            Ms = ms;
            Sampled = sampled;
            ImageFormat = imageFormat;
        }

        public override string ToString() => $"image<{SampledType}, dim={Dim}>";
    }

    #endregion

    #region SampledImage

    public sealed class SampledImageType : ShaderIrType
    {
        public ShaderIrType Image { get; }

        public SampledImageType(ShaderIrType image)
        {
            Image = image;
        }

        public override string ToString() => $"sampled_image<{Image}>";
    }

    #endregion

    #region AccelerationStructure

    public sealed class AccelerationStructureType : ShaderIrType
    {
        public override string ToString() => "acceleration_structure";
    }

    #endregion

    #region 静态工厂

    public static VoidType Void => new();
    public static BoolType Bool => new();
    public static IntType Int32 => new(32, true);
    public static IntType UInt32 => new(32, false);
    public static IntType Int64 => new(64, true);
    public static IntType UInt64 => new(64, false);
    public static FloatType Float32 => new(32);
    public static FloatType Float64 => new(64);

    public static VectorType Vec2(ShaderIrType? element = null) => new(element ?? Float32, 2);
    public static VectorType Vec3(ShaderIrType? element = null) => new(element ?? Float32, 3);
    public static VectorType Vec4(ShaderIrType? element = null) => new(element ?? Float32, 4);

    public static MatrixType Mat3(ShaderIrType? element = null) => new(element ?? Float32, 3, 3);
    public static MatrixType Mat4(ShaderIrType? element = null) => new(element ?? Float32, 4, 4);

    #endregion

    #region 辅助方法

    public bool IsFloat() => this is FloatType || (this is VectorType v && v.ElementType is FloatType);

    public bool IsInt() => this is IntType || (this is VectorType v && v.ElementType is IntType);

    public bool IsScalar() => this is FloatType or IntType or BoolType;

    public bool IsVector() => this is VectorType;

    public bool IsMatrix() => this is MatrixType;

    public int GetScalarSize() => this switch
    {
        FloatType f => (int)f.BitWidth,
        IntType i => (int)i.BitWidth,
        BoolType => 32,
        _ => 0
    };

    #endregion
}
