namespace Gnosis.IR.Graph;

public enum IrTypeKind
{
    Void,
    Bool,
    I8,
    I16,
    I32,
    I64,
    F32,
    F64,
    String,
    Struct,
    Array,
    Vector,
    Matrix,
    Function,
    Reference,
    Entity,
    Component,
    Query
}

public sealed class IrType
{
    #region Properties

    public IrTypeKind Kind { get; }

    public string? Name { get; }

    public int BitWidth { get; }

    public IReadOnlyList<IrType> GenericArgs { get; }

    public int ArrayLength { get; }

    public IReadOnlyList<(string Name, IrType Type)> Fields { get; }

    #endregion

    #region Static Instances

    public static IrType Void { get; } = new(IrTypeKind.Void);
    public static IrType Bool { get; } = new(IrTypeKind.Bool, bitWidth: 1);
    public static IrType I8 { get; } = new(IrTypeKind.I8, bitWidth: 8);
    public static IrType I16 { get; } = new(IrTypeKind.I16, bitWidth: 16);
    public static IrType I32 { get; } = new(IrTypeKind.I32, bitWidth: 32);
    public static IrType I64 { get; } = new(IrTypeKind.I64, bitWidth: 64);
    public static IrType F32 { get; } = new(IrTypeKind.F32, bitWidth: 32);
    public static IrType F64 { get; } = new(IrTypeKind.F64, bitWidth: 64);
    public static IrType String { get; } = new(IrTypeKind.String);
    public static IrType Entity { get; } = new(IrTypeKind.Entity);

    #endregion

    #region Constructors

    public IrType(IrTypeKind kind, string? name = null, int bitWidth = 0,
        IReadOnlyList<IrType>? genericArgs = null, int arrayLength = 0,
        IReadOnlyList<(string Name, IrType Type)>? fields = null)
    {
        Kind = kind;
        Name = name;
        BitWidth = bitWidth;
        GenericArgs = genericArgs ?? [];
        ArrayLength = arrayLength;
        Fields = fields ?? [];
    }

    #endregion

    #region Factory Methods

    public static IrType VectorOf(IrType elementType, int count)
    {
        return new IrType(IrTypeKind.Vector, genericArgs: [elementType], arrayLength: count);
    }

    public static IrType MatrixOf(IrType elementType, int rows, int cols)
    {
        return new IrType(IrTypeKind.Matrix, genericArgs: [elementType], arrayLength: rows * 100 + cols);
    }

    public static IrType ArrayOf(IrType elementType, int length = 0)
    {
        return new IrType(IrTypeKind.Array, genericArgs: [elementType], arrayLength: length);
    }

    public static IrType StructOf(string name, IReadOnlyList<(string Name, IrType Type)> fields)
    {
        return new IrType(IrTypeKind.Struct, name: name, fields: fields);
    }

    public static IrType FunctionOf(IrType returnType, IReadOnlyList<IrType> paramTypes)
    {
        var args = new List<IrType> { returnType };
        args.AddRange(paramTypes);
        return new IrType(IrTypeKind.Function, genericArgs: args);
    }

    public static IrType ReferenceTo(IrType inner)
    {
        return new IrType(IrTypeKind.Reference, genericArgs: [inner]);
    }

    public static IrType ComponentOf(string name, IReadOnlyList<(string Name, IrType Type)> fields)
    {
        return new IrType(IrTypeKind.Component, name: name, fields: fields);
    }

    public static IrType QueryOf(IrType componentType)
    {
        return new IrType(IrTypeKind.Query, genericArgs: [componentType]);
    }

    #endregion

    #region Public Methods

    public bool IsInteger() => Kind is IrTypeKind.I8 or IrTypeKind.I16 or IrTypeKind.I32 or IrTypeKind.I64;

    public bool IsFloat() => Kind is IrTypeKind.F32 or IrTypeKind.F64;

    public bool IsNumeric() => IsInteger() || IsFloat();

    public bool IsVector() => Kind == IrTypeKind.Vector;

    public bool IsMatrix() => Kind == IrTypeKind.Matrix;

    public bool IsStruct() => Kind == IrTypeKind.Struct;

    public bool IsArray() => Kind == IrTypeKind.Array;

    public bool IsFunction() => Kind == IrTypeKind.Function;

    public IrType GetElementType() => GenericArgs.Count > 0 ? GenericArgs[0] : Void;

    public IrType GetReturnType() => GenericArgs.Count > 0 ? GenericArgs[0] : Void;

    public IReadOnlyList<IrType> GetParameterTypes()
    {
        return GenericArgs.Count > 1 ? GenericArgs.Skip(1).ToList() : [];
    }

    public override string ToString()
    {
        return Kind switch
        {
            IrTypeKind.Void => "void",
            IrTypeKind.Bool => "bool",
            IrTypeKind.I8 => "i8",
            IrTypeKind.I16 => "i16",
            IrTypeKind.I32 => "i32",
            IrTypeKind.I64 => "i64",
            IrTypeKind.F32 => "f32",
            IrTypeKind.F64 => "f64",
            IrTypeKind.String => "string",
            IrTypeKind.Entity => "entity",
            IrTypeKind.Struct => Name ?? "struct",
            IrTypeKind.Array => ArrayLength > 0 ? $"[{GetElementType()}; {ArrayLength}]" : $"[{GetElementType()}]",
            IrTypeKind.Vector => $"vec{ArrayLength}<{GetElementType()}>",
            IrTypeKind.Matrix => $"mat{ArrayLength / 100}x{ArrayLength % 100}<{GetElementType()}>",
            IrTypeKind.Function => $"({string.Join(", ", GetParameterTypes())}) -> {GetReturnType()}",
            IrTypeKind.Reference => $"&{GetElementType()}",
            IrTypeKind.Component => Name ?? "component",
            IrTypeKind.Query => $"query<{GetElementType()}>",
            _ => Kind.ToString()
        };
    }

    #endregion
}
