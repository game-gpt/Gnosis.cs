namespace Gnosis.Rendering.ShaderCompiler.Backend.Spirv;

public sealed class SpirvTypeCache
{
    #region Fields

    private readonly SpirvBuilder _builder;
    private readonly Dictionary<string, uint> _typeIds = new();

    #endregion

    #region Constructors

    public SpirvTypeCache(SpirvBuilder builder)
    {
        _builder = builder;
    }

    #endregion

    #region Public Methods

    public uint GetVoidType()
    {
        return GetOrCreate("void", _builder.DeclareVoidType);
    }

    public uint GetBoolType()
    {
        return GetOrCreate("bool", _builder.DeclareBoolType);
    }

    public uint GetIntType(uint bitWidth = 32, bool signed = true)
    {
        var key = $"{(signed ? 'i' : 'u')}{bitWidth}";
        return GetOrCreate(key, () => _builder.DeclareIntType(bitWidth, signed ? 1u : 0u));
    }

    public uint GetFloatType(uint bitWidth = 32)
    {
        var key = $"f{bitWidth}";
        return GetOrCreate(key, () => _builder.DeclareFloatType(bitWidth));
    }

    public uint GetVectorType(uint elementType, uint componentCount)
    {
        var key = $"vec{componentCount}<{elementType}>";
        return GetOrCreate(key, () => _builder.DeclareVectorType(elementType, componentCount));
    }

    public uint GetMatrixType(uint columnType, uint columnCount)
    {
        var key = $"mat{columnCount}<{columnType}>";
        return GetOrCreate(key, () => _builder.DeclareMatrixType(columnType, columnCount));
    }

    public uint GetStructType(uint[] memberTypeIds, string name)
    {
        var key = $"struct:{name}";
        return GetOrCreate(key, () => _builder.DeclareStructType(memberTypeIds));
    }

    public uint GetImageType(uint sampledType, uint dim, uint depth = 0, uint arrayed = 0, uint ms = 0, uint format = 0)
    {
        var key = $"image<{sampledType},{dim},{depth},{arrayed},{ms},{format}>";
        return GetOrCreate(key, () => _builder.DeclareImageType(sampledType, dim, depth, arrayed, ms, format));
    }

    public uint GetSamplerType()
    {
        return GetOrCreate("sampler", _builder.DeclareSamplerType);
    }

    public uint GetSampledImageType(uint imageType)
    {
        var key = $"sampled_image<{imageType}>";
        return GetOrCreate(key, () => _builder.DeclareSampledImageType(imageType));
    }

    public uint GetPointerType(uint pointeeType, uint storageClass)
    {
        var key = $"ptr<{storageClass},{pointeeType}>";
        return GetOrCreate(key, () => _builder.DeclarePointerType(pointeeType, storageClass));
    }

    public uint GetFunctionType(uint returnType, uint[] parameterTypes)
    {
        var key = $"fn<{returnType}>[{string.Join(",", parameterTypes)}]";
        return GetOrCreate(key, () => _builder.DeclareFunctionType(returnType, parameterTypes));
    }

    public uint GetAccelerationStructureType()
    {
        return GetOrCreate("accel_struct", _builder.DeclareAccelerationStructureType);
    }

    #endregion

    #region Private Methods

    private uint GetOrCreate(string key, Func<uint> factory)
    {
        if (_typeIds.TryGetValue(key, out var id))
        {
            return id;
        }

        id = factory();
        _typeIds[key] = id;
        return id;
    }

    #endregion
}
