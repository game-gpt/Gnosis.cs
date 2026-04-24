using Gnosis.IR.Shader;

namespace Gnosis.Toolchain.ShaderCompiler;

public sealed class ShaderVariantCollection
{
    #region Fields

    private readonly Dictionary<string, ShaderVariant> _variants = new(StringComparer.Ordinal);

    #endregion

    #region Properties

    public string BaseShaderName { get; }

    public IReadOnlyList<ShaderVariant> Variants => _variants.Values.ToList().AsReadOnly();

    #endregion

    #region Constructors

    public ShaderVariantCollection(string baseShaderName)
    {
        BaseShaderName = baseShaderName;
    }

    #endregion

    #region Public Methods

    public ShaderVariant GetOrAddVariant(IReadOnlyDictionary<string, string> defines)
    {
        var key = ComputeVariantKey(defines);

        if (_variants.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var variant = new ShaderVariant(BaseShaderName, key, defines);
        _variants[key] = variant;
        return variant;
    }

    public void CollectVariantsFromModule(ShaderModuleIr module)
    {
        foreach (var func in module.Functions)
        {
            if (!func.IsEntryPoint)
            {
                continue;
            }

            var defines = ExtractDefinesFromAttributes(func.Attributes);
            var variant = GetOrAddVariant(defines);
            variant.AddEntryPoint(func.Name, func.EntryPointModel ?? ShaderExecutionModel.Vertex);
        }
    }

    public static string ComputeVariantKey(IReadOnlyDictionary<string, string> defines)
    {
        if (defines.Count == 0)
        {
            return "default";
        }

        var sortedPairs = defines.OrderBy(kv => kv.Key, StringComparer.Ordinal);
        return string.Join("_", sortedPairs.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    #endregion

    #region Private Methods

    private static Dictionary<string, string> ExtractDefinesFromAttributes(List<ShaderAttributeIr> attributes)
    {
        var defines = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var attr in attributes)
        {
            if (attr.Name == "Variant")
            {
                foreach (var arg in attr.Arguments)
                {
                    defines[arg] = "1";
                }
            }
            else if (attr.Name == "Define")
            {
                for (var i = 0; i + 1 < attr.Arguments.Count; i += 2)
                {
                    defines[attr.Arguments[i]] = attr.Arguments[i + 1];
                }
            }
        }

        return defines;
    }

    #endregion
}

public sealed class ShaderVariant
{
    #region Properties

    public string BaseShaderName { get; }
    public string VariantKey { get; }
    public IReadOnlyDictionary<string, string> Defines { get; }
    public List<ShaderVariantEntryPoint> EntryPoints { get; } = [];
    public byte[]? CompiledBytecode { get; set; }

    #endregion

    #region Constructors

    public ShaderVariant(string baseShaderName, string variantKey, IReadOnlyDictionary<string, string> defines)
    {
        BaseShaderName = baseShaderName;
        VariantKey = variantKey;
        Defines = defines;
    }

    #endregion

    #region Public Methods

    public void AddEntryPoint(string functionName, ShaderExecutionModel executionModel)
    {
        EntryPoints.Add(new ShaderVariantEntryPoint(functionName, executionModel));
    }

    #endregion
}

public sealed class ShaderVariantEntryPoint
{
    #region Properties

    public string FunctionName { get; }
    public ShaderExecutionModel ExecutionModel { get; }

    #endregion

    #region Constructors

    public ShaderVariantEntryPoint(string functionName, ShaderExecutionModel executionModel)
    {
        FunctionName = functionName;
        ExecutionModel = executionModel;
    }

    #endregion
}
