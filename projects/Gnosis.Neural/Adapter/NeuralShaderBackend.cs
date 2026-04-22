using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;

namespace Gnosis.Neural.Adapter;

/// <summary>
/// 神经网络着色器后端，将 Shader IR 转换为 Tensor Core 优化的计算着色器
/// </summary>
public sealed class NeuralShaderBackend : IShaderBackend
{
    #region Fields

    private readonly INeuralRuntime _neuralRuntime;

    #endregion

    #region Constructors

    public NeuralShaderBackend(INeuralRuntime neuralRuntime)
    {
        _neuralRuntime = neuralRuntime;
    }

    #endregion

    #region Public Methods

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

                case ShaderIrOpCode.Conv2D:
                    CompileConvolution(instruction, program);
                    break;

                case ShaderIrOpCode.Activation:
                    CompileActivation(instruction, program);
                    break;

                case ShaderIrOpCode.MatMul:
                    CompileTensorMatMul(instruction, program);
                    break;

                case ShaderIrOpCode.Attention:
                    CompileAttention(instruction, program);
                    break;

                case ShaderIrOpCode.LayerNorm:
                    CompileLayerNormalization(instruction, program);
                    break;

                case ShaderIrOpCode.Softmax:
                    CompileSoftmax(instruction, program);
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

    private void CompileConvolution(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Conv2D
        };

        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Batch", Size = 1 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Channels", Size = 3 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Height", Size = 224 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Width", Size = 224 });

        program.AddTensorOperation(tensorOp);
    }

    private void CompileActivation(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.ElementWiseRelu
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileTensorMatMul(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.MatMul
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileAttention(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Attention
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileLayerNormalization(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.LayerNorm
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileSoftmax(ShaderIrInstruction instruction, NeuralProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Softmax
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
