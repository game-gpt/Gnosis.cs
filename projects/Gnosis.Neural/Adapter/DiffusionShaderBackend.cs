using Gnosis.Graphic.Shader;
using Gnosis.IR.Shader;

namespace Gnosis.Neural.Adapter;

/// <summary>
/// 扩散模型着色器后端，将 Shader IR 转换为扩散模型优化的计算着色器
/// </summary>
public sealed class DiffusionShaderBackend : IShaderBackend
{
    #region Fields

    private readonly IDiffusionRuntime _diffusionRuntime;

    #endregion

    #region Constructors

    public DiffusionShaderBackend(IDiffusionRuntime diffusionRuntime)
    {
        _diffusionRuntime = diffusionRuntime;
    }

    #endregion

    #region Public Methods

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

                case ShaderIrOpCode.Conv2D:
                    CompileUNetConvolution(instruction, program);
                    break;

                case ShaderIrOpCode.Attention:
                    CompileCrossAttention(instruction, program);
                    break;

                case ShaderIrOpCode.GroupNorm:
                    CompileGroupNormalization(instruction, program);
                    break;

                case ShaderIrOpCode.Reshape:
                    CompileLatentReshape(instruction, program);
                    break;

                case ShaderIrOpCode.Interpolate:
                    CompileUpsample(instruction, program);
                    break;

                case ShaderIrOpCode.Sampler:
                    CompileTimestepSampler(instruction, program);
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

    private void CompileUNetConvolution(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Conv2D
        };

        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Batch", Size = 1 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Channels", Size = 4 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Height", Size = 64 });
        tensorOp.Dimensions.Add(new TensorDimensionIr { Name = "Width", Size = 64 });

        program.AddTensorOperation(tensorOp);
    }

    private void CompileCrossAttention(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Attention
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileGroupNormalization(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.LayerNorm
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileLatentReshape(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Reshape
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileUpsample(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Resize
        };

        program.AddTensorOperation(tensorOp);
    }

    private void CompileTimestepSampler(ShaderIrInstruction instruction, DiffusionProgram program)
    {
        var tensorOp = new TensorInstruction
        {
            OpCode = TensorOpCode.Gather
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
