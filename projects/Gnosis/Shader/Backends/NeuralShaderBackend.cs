using Gnosis.Compiler.Backend;
using Gnosis.Compiler.Backend.Spirv;
using Gnosis.Compiler.Backend.ShaderIR;
using Gnosis.Compiler.ValueObjects.AST;
using GnosisEngine.Shader;
using GnosisEngine.Shader.ValueObjects;

namespace GnosisEngine.Shader.Backends;

public sealed class NeuralShaderBackend : IShaderBackend
{
    #region Properties

    public string Name => "Neural";

    #endregion

    #region Public Methods

    public bool SupportsKind(MicroFunctionKind kind) =>
        kind is MicroFunctionKind.Neural or MicroFunctionKind.Vertex or MicroFunctionKind.Fragment;

    public IShaderModule CompileModule(IShaderModule module, ShaderCompileOptions options)
    {
        if (module is not DelegateShaderModule delegateModule)
        {
            return module;
        }

        var externalRefs = new List<ExternalFunctionRef>();
        foreach (var func in delegateModule.Functions)
        {
            if (func is DelegateMicroFunction { Execute: null })
            {
                externalRefs.Add(new ExternalFunctionRef(
                    func.Name,
                    func.InputParameters.Select(p => MapParameterType(p.Type)).ToList(),
                    MapParameterType(func.OutputParameter.Type)));
            }
        }

        var ir = new ShaderModuleIr(
            module.Name,
            delegateModule.Functions.Select(MapFunction).ToList(),
            new List<ShaderStructIr>(),
            new List<ShaderGlobalVariableIr>(),
            new List<ShaderEntryPointIr>(),
            externalRefs);

        var spirvGenerator = new SpirvGenerator();
        var bytecode = spirvGenerator.Generate(ir);

        return new DelegateShaderModule(
            module.Name,
            delegateModule.Functions,
            module.Language,
            module.Target)
        {
        };
    }

    public void DispatchCompute(IMicroFunction computeFunction, uint groupCountX, uint groupCountY, uint groupCountZ)
    {
    }

    #endregion

    #region Private Methods

    private static ShaderFunctionIr MapFunction(IMicroFunction func)
    {
        var executionModel = func.Kind switch
        {
            MicroFunctionKind.Vertex => ShaderExecutionModel.Vertex,
            MicroFunctionKind.Fragment => ShaderExecutionModel.Fragment,
            MicroFunctionKind.Neural => ShaderExecutionModel.Fragment,
            _ => (ShaderExecutionModel?)null
        };

        return new ShaderFunctionIr(
            func.Name,
            executionModel,
            func.InputParameters.Select(p => new ShaderIrParameter(p.Name, MapParameterType(p.Type))).ToList(),
            MapParameterType(func.OutputParameter.Type),
            new List<ShaderIrInstruction>(),
            new List<LocalVariableInstruction>(),
            new List<AttributeDecl>(),
            executionModel != null);
    }

    private static ShaderIrType MapParameterType(ShaderParameterType type) => type switch
    {
        ShaderParameterType.Float32 => new ShaderIrType.FloatType(),
        ShaderParameterType.Vec2 => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 2),
        ShaderParameterType.Vec3 => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 3),
        ShaderParameterType.Vec4 => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 4),
        ShaderParameterType.Int32 => new ShaderIrType.IntType(),
        ShaderParameterType.Mat4 => new ShaderIrType.MatrixType(new ShaderIrType.FloatType(), 4, 4),
        ShaderParameterType.Texture2D => new ShaderIrType.ImageType(new ShaderIrType.FloatType(), 1, 0, false, false, 0),
        ShaderParameterType.Sampler => new ShaderIrType.SamplerType(),
        ShaderParameterType.NeuralModel => new ShaderIrType.ExternalType("neural_model"),
        _ => new ShaderIrType.VoidType()
    };

    #endregion
}
