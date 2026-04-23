using Acorn.Tensor.Data;
using Acorn.Tensor.Encode;
using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Adapter;

/// <summary>
///     扩散模型着色器后端，将 Shader IR 转换为扩散模型优化的计算着色器。
/// </summary>
/// <remarks>
///     本后端使用 Acorn.Tensor 的 <see cref="TensorProgramEncoder" /> 进行张量程序二进制编码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class DiffusionShaderBackend : IShaderBackend
{
    #region Fields

    private readonly IDiffusionRuntime _diffusionRuntime;
    private readonly List<TensorInstruction> _operations = [];

    #endregion

    #region Properties

    /// <summary>
    ///     后端名称
    /// </summary>
    public string Name => "Diffusion";

    #endregion

    #region Constructors

    public DiffusionShaderBackend(IDiffusionRuntime diffusionRuntime)
    {
        _diffusionRuntime = diffusionRuntime;
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
    ///     编译 Shader IR 为扩散模型优化的计算着色器
    /// </summary>
    public byte[] Compile(ShaderModuleIr module, ShaderCompileOptions options)
    {
        _operations.Clear();

        foreach (var function in module.Functions)
        {
            if (function.IsEntryPoint && function.EntryPointModel == ShaderExecutionModel.Compute)
            {
                CompileDiffusionFunction(function);
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

    private void CompileDiffusionFunction(ShaderFunctionIr function)
    {
        foreach (var instruction in function.Instructions)
        {
            switch (instruction.OpCode)
            {
                case ShaderIrOpCode.Mul:
                    CompileNoiseMultiply(instruction);
                    break;

                case ShaderIrOpCode.Add:
                    CompileNoiseAddition(instruction);
                    break;

                case ShaderIrOpCode.Sub:
                    CompileDenoiseStep(instruction);
                    break;

                case ShaderIrOpCode.Div:
                    CompileNoiseDivide(instruction);
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
                    CompileGenericDiffusionInstruction(instruction);
                    break;
            }
        }
    }

    private void CompileNoiseMultiply(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseMul
        });
    }

    private void CompileNoiseAddition(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        });
    }

    private void CompileDenoiseStep(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSub
        });
    }

    private void CompileNoiseDivide(ShaderIrInstruction instruction)
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

    private void CompileGenericDiffusionInstruction(ShaderIrInstruction instruction)
    {
        _operations.Add(new TensorInstruction
        {
            OpCode = TensorOpCode.Custom
        });
    }

    #endregion
}
