using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Adapter;

/// <summary>
/// 神经网络着色器后端，将 Shader IR 转换为 Tensor Core 优化的计算着色器
/// </summary>
public sealed class NeuralShaderBackend : IShaderBackend
{
    #region Fields

    private readonly INeuralRuntime _neuralRuntime;

    #endregion

    #region Properties

    /// <summary>
    /// 后端名称
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
    /// 编译 Shader IR 为神经网络优化的计算着色器
    /// </summary>
    public byte[] Compile(ShaderModuleIr module, ShaderCompileOptions options)
    {
        var neuralProgram = new NeuralProgram();

        foreach (var function in module.Functions)
        {
            if (function.IsEntryPoint && function.EntryPointModel == ShaderExecutionModel.Compute)
            {
                CompileNeuralFunction(function, neuralProgram);
            }
        }

        return neuralProgram.Serialize();
    }

    /// <summary>
    /// 分发计算着色器
    /// </summary>
    public void DispatchCompute(IMicroFunction computeFunction, uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        // 神经网络后端通过运行时执行计算
    }

    #endregion

    #region Private Methods

    private void CompileNeuralFunction(ShaderFunctionIr function, NeuralProgram program)
    {
        foreach (var instruction in function.Instructions)
        {
            switch (instruction.OpCode)
            {
                case ShaderIrOpCode.Mul:
                    CompileMatrixMultiply(instruction, program);
                    break;

                case ShaderIrOpCode.Add:
                    CompileElementWiseAdd(instruction, program);
                    break;

                case ShaderIrOpCode.Sub:
                    CompileElementWiseSub(instruction, program);
                    break;

                case ShaderIrOpCode.Div:
                    CompileElementWiseDiv(instruction, program);
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
                    CompileGenericInstruction(instruction, program);
                    break;
            }
        }
    }

    private void CompileMatrixMultiply(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.MatMul
        };

        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "M", Size = 1024 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "N", Size = 1024 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "K", Size = 1024 });

        program.AddTensorOperation(tensorOp);
    }

    private void CompileElementWiseAdd(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileElementWiseSub(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSub
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileElementWiseDiv(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseDiv
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileDotProduct(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ReduceSum
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileCrossProduct(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseMul
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileInterpolation(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseAdd
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileStepFunction(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseClamp
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileSmoothStepFunction(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseSigmoid
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileGenericInstruction(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Custom
        };

        program.AddTensorOperation(tensorOp);
    }

    #endregion

    #region Nested Types

    private sealed class NeuralProgram
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
