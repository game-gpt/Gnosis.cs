namespace Gnosis.Runtime.Reflection;

/// <summary>
/// gg 类型种类
/// </summary>
public enum GGTypeKind : byte
{
    Primitive,
    Struct,
    Class,
    Array,
    Function,
    Closure,
    Native
}

/// <summary>
/// gg 类型运行时表示
/// </summary>
public sealed class GGType
{
    #region Properties

    /// <summary>
    /// 类型名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 类型种类
    /// </summary>
    public GGTypeKind Kind { get; }

    /// <summary>
    /// 类型大小（字节）
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// 字段列表
    /// </summary>
    public IReadOnlyList<GGFieldInfo> Fields { get; }

    /// <summary>
    /// 方法列表
    /// </summary>
    public IReadOnlyList<GGMethodInfo> Methods { get; }

    /// <summary>
    /// 所属模块
    /// </summary>
    public string? ModuleName { get; }

    /// <summary>
    /// 是否为值类型
    /// </summary>
    public bool IsValueType => Kind is GGTypeKind.Primitive or GGTypeKind.Struct;

    /// <summary>
    /// 是否为引用类型
    /// </summary>
    public bool IsReferenceType => !IsValueType;

    #endregion

    #region Constructors

    public GGType(string name, GGTypeKind kind, int size = 0,
        IReadOnlyList<GGFieldInfo>? fields = null,
        IReadOnlyList<GGMethodInfo>? methods = null,
        string? moduleName = null)
    {
        Name = name;
        Kind = kind;
        Size = size;
        Fields = fields ?? [];
        Methods = methods ?? [];
        ModuleName = moduleName;
    }

    #endregion

    #region 预定义类型

    public static readonly GGType Null = new("null", GGTypeKind.Primitive, 0);
    public static readonly GGType Int = new("int", GGTypeKind.Primitive, 8);
    public static readonly GGType Float = new("float", GGTypeKind.Primitive, 8);
    public static readonly GGType Bool = new("bool", GGTypeKind.Primitive, 1);
    public static readonly GGType String = new("string", GGTypeKind.Class, 16);
    public static readonly GGType Object = new("object", GGTypeKind.Class, 16);
    public static readonly GGType Array = new("array", GGTypeKind.Array, 16);
    public static readonly GGType Closure = new("closure", GGTypeKind.Closure, 24);

    #endregion

    #region 方法

    /// <summary>
    /// 查找字段
    /// </summary>
    public GGFieldInfo? GetField(string name)
    {
        foreach (var field in Fields)
        {
            if (field.Name == name)
            {
                return field;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找方法
    /// </summary>
    public GGMethodInfo? GetMethod(string name)
    {
        foreach (var method in Methods)
        {
            if (method.Name == name)
            {
                return method;
            }
        }

        return null;
    }

    public override string ToString()
    {
        return $"{Kind} {Name}";
    }

    #endregion
}

/// <summary>
/// gg 字段信息
/// </summary>
public sealed class GGFieldInfo
{
    #region Properties

    public string Name { get; }
    public GGType FieldType { get; }
    public int Offset { get; }

    #endregion

    #region Constructors

    public GGFieldInfo(string name, GGType fieldType, int offset = 0)
    {
        Name = name;
        FieldType = fieldType;
        Offset = offset;
    }

    #endregion
}

/// <summary>
/// gg 方法信息
/// </summary>
public sealed class GGMethodInfo
{
    #region Properties

    public string Name { get; }
    public GGType DeclaringType { get; }
    public GGType ReturnType { get; }
    public IReadOnlyList<GGParameterInfo> Parameters { get; }
    public int ParameterCount => Parameters.Count;
    public int EntryOffset { get; }

    #endregion

    #region Constructors

    public GGMethodInfo(string name, GGType declaringType, GGType returnType,
        IReadOnlyList<GGParameterInfo>? parameters = null, int entryOffset = 0)
    {
        Name = name;
        DeclaringType = declaringType;
        ReturnType = returnType;
        Parameters = parameters ?? [];
        EntryOffset = entryOffset;
    }

    #endregion
}

/// <summary>
/// gg 参数信息
/// </summary>
public sealed class GGParameterInfo
{
    #region Properties

    public string Name { get; }
    public GGType ParameterType { get; }

    #endregion

    #region Constructors

    public GGParameterInfo(string name, GGType parameterType)
    {
        Name = name;
        ParameterType = parameterType;
    }

    #endregion
}
