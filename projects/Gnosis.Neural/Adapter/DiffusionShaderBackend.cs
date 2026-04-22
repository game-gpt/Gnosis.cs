using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Adapter;

/// <summary>
/// 扩散模型着色器后端，将 Shader IR 转换为扩散模型优化的计算着色器
/// </summary>
public sealed class DiffusionShaderBackend : IShaderBackend
{
    #region Fields

    private readonly IDiffusionRuntime _diffusionRuntime;

    #endregion

    #region Properties

    /// <summary>
    /// 后端名称
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
    /// 检查是否支持指定的微函数类型
    /// </summary>
    public bool SupportsKind(MicroFunctionKind kind)
    {
        return kind is MicroFunctionKind.Compute;
    }

    /// <summary>
    /// 编译着色器模块
    /// </summary>
    public IShaderModule CompileModule(IShaderModule module, ShaderCompileOptions options)
    {
        return module;
    }

    /// <summary>
    /// 编译 Shader IR 为扩散模型优化的计算着色器
    /// </summary>
    public byte[] Compile(ShaderModuleIr module, ShaderCompileOptions options)
    {
        var diffusionProgram = new DiffusionProgram();

        foreach (var function in module.Functions)
        {
            if (function.IsEntryPoint && function.EntryPointModel == ShaderExecutionModel.Compute)
            {
                CompileDiffusionFunction(function, diffusionProgram);
            }
        }

        return diffusionProgram.Serialize();
    }

    /// <summary>
    /// 分发计算着色器
    /// </summary>
    public void DispatchCompute(IMicroFunction computeFunction, uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        // 扩散模型后端通过运行时执行计算
    }

    #endregion

    #region Private Methods

    private void CompileDiffusionFunction(ShaderFunctionIr function, DiffusionProgram program)
    {
        foreach (var instruction in function.Instructions)
        {
            switch (instruction.OpCode)
            {
                case ShaderIrOpCode.Mul:
                    CompileNoiseMultiply(instruction, program);
                    break;

                case ShaderIrOpCode.Add:
                    CompileNoiseAddition(instruction, program);
                    break;

                case ShaderIrOpCode.Sub:
                    CompileDenoiseStep(instruction, program);
                    break;

                case ShaderIrOpCode.Div:
                    CompileNoiseDivide(instruction, program);
                    break;

                case ShaderIrOpCode.Dot:
                    CompileDotProduct(instruction, program);
                    break;

                case ShaderIrOpCode.Cross:
                    CompileCrossProduct(instruction, program);
                    break;

                case ShaderIrOpCode.Lerp:
                    CompileInterpolation(instruction, program);
                    break;

                case ShaderIrOpCode.Step:
                    CompileStepFunction(instruction, program);
                    break;

                case ShaderIrOpCode.SmoothStep:
                    CompileSmoothStepFunction(instruction, program);
                    break;

                default:
                    CompileGenericDiffusionInstruction(instruction, program);
                    break;
            }
        }
    }

    private void CompileNoiseMultiply(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseMul
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileNoiseAddition(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileDenoiseStep(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSub
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileNoiseDivide(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseDiv
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileDotProduct(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ReduceSum
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileCrossProduct(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseMul
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileInterpolation(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileStepFunction(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseClamp
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileSmoothStepFunction(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSigmoid
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileGenericDiffusionInstruction(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Custom
        };

        program.AddTensorOperation(tensorOp);
    }

    #endregion

    #region Nested Types

    private sealed class DiffusionProgram
    {
        private readonly List<TensorInstruction> _operations = [];

        public void AddTensorOperation(TensorInstruction operation)
        {
            _operations.Add(operation);
        }

        public byte[] Serialize()
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);

            writer.Write(_operations.Count);

            foreach (var op in _operations)
            {
                writer.Write((int)op.OpCode);
                writer.Write(op.Dimensions.Count);

                foreach (var dim in op.Dimensions)
                {
                    writer.Write(dim.Name);
                    writer.Write(dim.Size);
                    writer.Write(dim.Stride);
                }
            }

            return stream.ToArray();
        }
    }

    #endregion
}
