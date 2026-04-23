using Acorn.Tensor.Data;
using Acorn.Tensor.Encode;
using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Adapter;

/// <summary>
///     神经网络着色器后端，将 Shader IR 转换为 Tensor Core 优化的计算着色器。
/// </summary>
/// <remarks>
///     本后端使用 Acorn.Tensor 的 <see cref="TensorProgramEncoder" /> 进行张量程序二进制编码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class NeuralShaderBackend : IShaderBackend
{
    #region Fields

    private readonly INeuralRuntime _neuralRuntime;
    private readonly List<TensorInstruction> _operations = [];

    #endregion

    #region Properties

    /// <summary>
    ///     后端名称
    /// </summary>
    public string Name => "Neural";

    #endregion

    #region Constructors

    public NeuralShaderBackend(INeuralRuntime neuralRuntime)
    {
        _neuralRuntime = neuralRuntime;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     检查是否支持指定的微函数类型
    /// </summary>
    public bool SupportsKind(MicroFunctionKind kind)
    {
        return kind is MicroFunctionKind.Compute;
    }

    /// <summary>
    ///     编译着色器模块
    /// </summary>
    public IShaderModule CompileModule(IShaderModule module, ShaderCompileOptions options)
    {
        return module;
    }

    /// <summary>
    ///     编译 Shader IR 为神经网络优化的计算着色器
    /// </summary>
    public byte[] Compile(ShaderModuleIr module, ShaderCompileOptions options)
    {
        _operations.Clear();

        foreach (var function in module.Functions)
        {
            if (function.IsEntryPoint && function.EntryPointModel == ShaderExecutionModel.Compute)
            {
                CompileNeuralFunction(function);
            }
        }

        var program = new TensorProgramData
        {
            Operations = _operations.ToArray()
        };

        var encoder = new TensorProgramEncoder();
        return encoder.Encode(program);
    }

    /// <summary>
    ///     分发计算着色器
    /// </summary>
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
                    CompileMatrixMultiply(instruction);
                    break;

                case ShaderIrOpCode.Add:
                    CompileElementWiseAdd(instruction);
                    break;

                case ShaderIrOpCode.Sub:
                    CompileElementWiseSub(instruction);
                    break;

                case ShaderIrOpCode.Div:
                    CompileElementWiseDiv(instruction);
                    break;

                case ShaderIrOpCode.Dot:
                    CompileDotProduct(instruction);
                    break;

                case ShaderIrOpCode.Cross:
                    CompileCrossProduct(instruction);
                    break;

                case ShaderIrOpCode.Lerp:
                    CompileInterpolation(instruction);
                    break;

                case ShaderIrOpCode.Step:
                    CompileStepFunction(instruction);
                    break;

                case ShaderIrOpCode.SmoothStep:
                    CompileSmoothStepFunction(instruction);
                    break;

                default:
                    CompileGenericInstruction(instruction);
                    break;
            }
        }
    }

    private void CompileMatrixMultiply(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.MatMul,
            Dimensions =
            [
                new TensorDimension { Name = "M", Size = 1024 },
                new TensorDimension { Name = "N", Size = 1024 },
                new TensorDimension { Name = "K", Size = 1024 }
            ]
        });
    }

    private void CompileElementWiseAdd(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        });
    }

    private void CompileElementWiseSub(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSub
        });
    }

    private void CompileElementWiseDiv(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseDiv
        });
    }

    private void CompileDotProduct(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ReduceSum
        });
    }

    private void CompileCrossProduct(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseMul
        });
    }

    private void CompileInterpolation(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        });
    }

    private void CompileStepFunction(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseClamp
        });
    }

    private void CompileSmoothStepFunction(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSigmoid
        });
    }

    private void CompileGenericInstruction(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.Custom
        });
    }

    #endregion
}
