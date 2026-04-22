using Acorn.Spirv.Data;

namespace Gnosis.Graphic.Shader.Spirv;

/// <summary>
///     SPIR-V 类型缓存，缓存 SPIR-V 类型声明以避免重复声明相同类型。
/// </summary>
/// <remarks>
///     本缓存使用 Acorn.Spirv 的 <see cref="SpirvConstants" /> 常量，
///     遵循架构规则：二进制编解码常量由 Acorn 独占。
/// </remarks>
public sealed class SpirvTypeCache
{
    private readonly SpirvBuilder _builder;
    private readonly Dictionary<string, uint> _typeCache = [];

    private uint _voidTypeId;
    private uint _boolTypeId;
    private uint _int32TypeId;
    private uint _uint32TypeId;
    private uint _float32TypeId;
    private uint _glslStd450Id;

    /// <summary>
    ///     初始化 <see cref="SpirvTypeCache" /> 的新实例。
    /// </summary>
    /// <param name="builder">SPIR-V 构建器。</param>
    public SpirvTypeCache(SpirvBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>
    ///     获取 void 类型 ID。
    /// </summary>
    public uint VoidType => GetOrCreate(ref _voidTypeId, "void", () =>
    {
        var id = _builder.AllocateId();
        _builder.DeclareVoidType(id);
        return id;
    });

    /// <summary>
    ///     获取 bool 类型 ID。
    /// </summary>
    public uint BoolType => GetOrCreate(ref _boolTypeId, "bool", () =>
    {
        var id = _builder.AllocateId();
        _builder.DeclareBoolType(id);
        return id;
    });

    /// <summary>
    ///     获取 32 位有符号整数类型 ID。
    /// </summary>
    public uint Int32Type => GetOrCreate(ref _int32TypeId, "int32", () =>
    {
        var id = _builder.AllocateId();
        _builder.DeclareIntType(id, 32, true);
        return id;
    });

    /// <summary>
    ///     获取 32 位无符号整数类型 ID。
    /// </summary>
    public uint UInt32Type => GetOrCreate(ref _uint32TypeId, "uint32", () =>
    {
        var id = _builder.AllocateId();
        _builder.DeclareIntType(id, 32, false);
        return id;
    });

    /// <summary>
    ///     获取 32 位浮点类型 ID。
    /// </summary>
    public uint Float32Type => GetOrCreate(ref _float32TypeId, "float32", () =>
    {
        var id = _builder.AllocateId();
        _builder.DeclareFloatType(id, 32);
        return id;
    });

    /// <summary>
    ///     获取 GLSL.std.450 扩展指令集 ID。
    /// </summary>
    public uint GLSLStd450 => GetOrCreate(ref _glslStd450Id, "GLSLStd450", () =>
    {
        var id = _builder.AllocateId();
        _builder.AddExtInstImport(id, "GLSL.std.450");
        return id;
    });

    /// <summary>
    ///     获取或创建向量类型 ID。
    /// </summary>
    /// <param name="componentTypeId">分量类型 ID。</param>
    /// <param name="componentCount">分量数量。</param>
    /// <returns>向量类型 ID。</returns>
    public uint GetVectorType(uint componentTypeId, uint componentCount)
    {
        var key = $"vec_{componentTypeId}_{componentCount}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareVectorType(id, componentTypeId, componentCount);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建矩阵类型 ID。
    /// </summary>
    /// <param name="columnTypeId">列类型 ID。</param>
    /// <param name="columnCount">列数量。</param>
    /// <returns>矩阵类型 ID。</returns>
    public uint GetMatrixType(uint columnTypeId, uint columnCount)
    {
        var key = $"mat_{columnTypeId}_{columnCount}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareMatrixType(id, columnTypeId, columnCount);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建指针类型 ID。
    /// </summary>
    /// <param name="storageClass">存储类。</param>
    /// <param name="typeId">指向的类型 ID。</param>
    /// <returns>指针类型 ID。</returns>
    public uint GetPointerType(uint storageClass, uint typeId)
    {
        var key = $"ptr_{storageClass}_{typeId}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclarePointerType(id, storageClass, typeId);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建数组类型 ID。
    /// </summary>
    /// <param name="elementTypeId">元素类型 ID。</param>
    /// <param name="lengthId">长度 ID。</param>
    /// <returns>数组类型 ID。</returns>
    public uint GetArrayType(uint elementTypeId, uint lengthId)
    {
        var key = $"arr_{elementTypeId}_{lengthId}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareArrayType(id, elementTypeId, lengthId);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建运行时数组类型 ID。
    /// </summary>
    /// <param name="elementTypeId">元素类型 ID。</param>
    /// <returns>运行时数组类型 ID。</returns>
    public uint GetRuntimeArrayType(uint elementTypeId)
    {
        var key = $"rtarr_{elementTypeId}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareRuntimeArrayType(id, elementTypeId);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建结构体类型 ID。
    /// </summary>
    /// <param name="memberTypeIds">成员类型 ID 列表。</param>
    /// <returns>结构体类型 ID。</returns>
    public uint GetStructType(uint[] memberTypeIds)
    {
        var key = $"struct_{string.Join("_", memberTypeIds)}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareStructType(id, memberTypeIds);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建函数类型 ID。
    /// </summary>
    /// <param name="returnTypeId">返回类型 ID。</param>
    /// <param name="parameterTypeIds">参数类型 ID 列表。</param>
    /// <returns>函数类型 ID。</returns>
    public uint GetFunctionType(uint returnTypeId, uint[]? parameterTypeIds = null)
    {
        var paramKey = parameterTypeIds is not null ? string.Join("_", parameterTypeIds) : "none";
        var key = $"func_{returnTypeId}_{paramKey}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareFunctionType(id, returnTypeId, parameterTypeIds);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建图像类型 ID。
    /// </summary>
    public uint GetImageType(uint sampledTypeId, uint dim, uint depth, uint arrayed, uint ms, uint sampled, uint imageFormat)
    {
        var key = $"img_{sampledTypeId}_{dim}_{depth}_{arrayed}_{ms}_{sampled}_{imageFormat}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareImageType(id, sampledTypeId, dim, depth, arrayed, ms, sampled, imageFormat);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建采样器类型 ID。
    /// </summary>
    public uint GetSamplerType()
    {
        return GetOrCreate("sampler", () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareSamplerType(id);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建采样图像类型 ID。
    /// </summary>
    public uint GetSampledImageType(uint imageTypeId)
    {
        var key = $"sampled_img_{imageTypeId}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareSampledImageType(id, imageTypeId);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建加速结构类型 ID（光线追踪）。
    /// </summary>
    public uint GetAccelerationStructureType()
    {
        return GetOrCreate("accel_struct", () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareAccelerationStructureType(id);
            return id;
        });
    }

    /// <summary>
    ///     获取 void 类型 ID（便捷别名）。
    /// </summary>
    public uint GetVoidType() => VoidType;

    /// <summary>
    ///     获取 bool 类型 ID（便捷别名）。
    /// </summary>
    public uint GetBoolType() => BoolType;

    /// <summary>
    ///     获取整数类型 ID。
    /// </summary>
    /// <param name="bitWidth">位宽。</param>
    /// <param name="signed">是否有符号。</param>
    /// <returns>整数类型 ID。</returns>
    public uint GetIntType(uint bitWidth, bool signed)
    {
        var key = $"int_{bitWidth}_{signed}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareIntType(id, bitWidth, signed);
            return id;
        });
    }

    /// <summary>
    ///     获取浮点类型 ID。
    /// </summary>
    /// <param name="bitWidth">位宽。</param>
    /// <returns>浮点类型 ID。</returns>
    public uint GetFloatType(uint bitWidth)
    {
        var key = $"float_{bitWidth}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareFloatType(id, bitWidth);
            return id;
        });
    }

    /// <summary>
    ///     获取或创建结构体类型 ID（带名称）。
    /// </summary>
    /// <param name="memberTypeIds">成员类型 ID 列表。</param>
    /// <param name="name">结构体名称（用于缓存键）。</param>
    /// <returns>结构体类型 ID。</returns>
    public uint GetStructType(uint[] memberTypeIds, string name)
    {
        var key = $"struct_{name}_{string.Join("_", memberTypeIds)}";
        return GetOrCreate(key, () =>
        {
            var id = _builder.AllocateId();
            _builder.DeclareStructType(id, memberTypeIds);
            return id;
        });
    }

    private uint GetOrCreate(ref uint field, string key, Func<uint> factory)
    {
        if (field != 0)
        {
            return field;
        }

        if (_typeCache.TryGetValue(key, out var cachedId))
        {
            field = cachedId;
            return cachedId;
        }

        var id = factory();
        _typeCache[key] = id;
        field = id;
        return id;
    }

    private uint GetOrCreate(string key, Func<uint> factory)
    {
        if (_typeCache.TryGetValue(key, out var cachedId))
        {
            return cachedId;
        }

        var id = factory();
        _typeCache[key] = id;
        return id;
    }
}
