using Acorn.SafeTensors.Data;
using Acorn.SafeTensors.Encode;
using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Adapter;

/// <summary>
///     神经网络着色器后端，将 Shader IR 转换为 SafeTensors 格式的模型权重。
/// </summary>
/// <remarks>
///     本后端使用 Acorn.SafeTensors 的 <see cref="SafeTensorsEncoder" /> 进行模型权重编码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class NeuralShaderBackend : IShaderBackend
{
    #region Fields

    private readonly INeuralRuntime _neuralRuntime;
    private readonly List<SafeTensorData> _tensors = [];

    #endregion

    #region Properties

    public string Name => "Neural";

    #endregion

    #region Constructors

    public NeuralShaderBackend(INeuralRuntime neuralRuntime)
    {
        _neuralRuntime = neuralRuntime;
    }

    #endregion

    #region Public Methods

    public bool SupportsKind(MicroFunctionKind kind)
    {
        return kind is MicroFunctionKind.Compute;
    }

    public IShaderModule CompileModule(IShaderModule module, ShaderCompileOptions options)
    {
        return module;
    }

    /// <summary>
    ///     编译 Shader IR 为 SafeTensors 格式的模型权重。
    /// </summary>
    public byte[] Compile(ShaderModuleIr module, ShaderCompileOptions options)
    {
        _tensors.Clear();

        foreach (var function in module.Functions)
        {
            if (function.IsEntryPoint && function.EntryPointModel == ShaderExecutionModel.Compute)
            {
                CompileNeuralFunction(function);
            }
        }

        var encoder = new SafeTensorsEncoder();
        return encoder.Encode(_tensors);
    }

    public void DispatchCompute(IMicroFunction computeFunction, uint groupCountX, uint groupCountY, uint groupCountZ)
    {
    }

    #endregion

    #region Private Methods

    private void CompileNeuralFunction(ShaderFunctionIr function)
    {
        foreach (var instruction in function.Instructions)
        {
            switch (instruction.OpCode)
            {
                case ShaderIrOpCode.Mul:
                    AddWeight("matmul_weight", SafeTensorDType.Float32, [1024, 1024]);
                    break;

                case ShaderIrOpCode.Add:
                    AddWeight("bias", SafeTensorDType.Float32, [1024]);
                    break;

                case ShaderIrOpCode.Dot:
                    AddWeight("dot_weight", SafeTensorDType.Float32, [1024]);
                    break;

                default:
                    break;
            }
        }
    }

    private void AddWeight(string name, SafeTensorDType dtype, long[] shape)
    {
        var elementSize = dtype switch
        {
            SafeTensorDType.Float32 => 4,
            SafeTensorDType.Float16 => 2,
            SafeTensorDType.Float64 => 8,
            SafeTensorDType.Int32 => 4,
            SafeTensorDType.Int64 => 8,
            SafeTensorDType.BFloat16 => 2,
            _ => 4
        };

        var totalElements = shape.Aggregate(1L, (a, b) => a * b);
        var data = new byte[totalElements * elementSize];

        _tensors.Add(new SafeTensorData
        {
            Name = name,
            DType = dtype,
            Shape = shape,
            Data = data
        });
    }

    #endregion
}
