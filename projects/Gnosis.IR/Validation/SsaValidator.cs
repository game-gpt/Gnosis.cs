using Gnosis.IR.Analysis;
using Gnosis.IR.Graph;

namespace Gnosis.IR.Validation;

public sealed class SsaValidator
{
    #region Properties

    public IrModule Module { get; }

    public IReadOnlyList<ValidationError> Errors => _errors;

    public IReadOnlyList<ValidationError> Warnings => _warnings;

    public bool IsValid => _errors.Count == 0;

    #endregion

    #region Fields

    private readonly List<ValidationError> _errors = [];
    private readonly List<ValidationError> _warnings = [];

    #endregion

    #region Constructors

    public SsaValidator(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Validate()
    {
        _errors.Clear();
        _warnings.Clear();

        foreach (var function in Module.Functions)
        {
            ValidateFunction(function);
        }

        return IsValid;
    }

    #endregion

    #region Private Methods

    private void ValidateFunction(IrFunction function)
    {
        ValidateSingleDefinition(function);
        ValidateDominanceProperty(function);
        ValidatePhiCompleteness(function);
        ValidateOperandDominance(function);
    }

    private void ValidateSingleDefinition(IrFunction function)
    {
        var definitions = new Dictionary<IrValue, (IrFunction Function, BasicBlock Block, IrInstruction Instruction)>();

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is null)
                {
                    continue;
                }

                if (definitions.TryGetValue(instruction.Result, out var existing))
                {
                    _errors.Add(new ValidationError("SSA_E001",
                        $"值 '{instruction.Result}' 在 SSA 中被多次定义：" +
                        $"第一次在 {existing.Block.Label}，第二次在 {block.Label}",
                        function, block, instruction));
                }
                else
                {
                    definitions[instruction.Result] = (function, block, instruction);
                }
            }
        }
    }

    private void ValidateDominanceProperty(IrFunction function)
    {
        var domTree = new DominatorTree(function);
        var definitions = new Dictionary<IrValue, BasicBlock>();

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null)
                {
                    definitions[instruction.Result] = block;
                }
            }
        }

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Opcode == IrOpcode.Phi)
                {
                    continue;
                }

                foreach (var operand in instruction.Operands)
                {
                    if (!definitions.TryGetValue(operand, out var defBlock))
                    {
                        continue;
                    }

                    if (!domTree.Dominates(defBlock, block))
                    {
                        _errors.Add(new ValidationError("SSA_E002",
                            $"值 '{operand}' 的定义块 '{defBlock.Label}' 不支配使用块 '{block.Label}'",
                            function, block, instruction));
                    }
                }
            }
        }
    }

    private void ValidatePhiCompleteness(IrFunction function)
    {
        foreach (var block in function.Blocks)
        {
            if (block.Predecessors.Count < 2)
            {
                continue;
            }

            var phiValues = new HashSet<IrValue>();
            foreach (var instruction in block.GetPhiNodes())
            {
                if (instruction.Result is not null)
                {
                    phiValues.Add(instruction.Result);
                }
            }

            var incomingValues = new Dictionary<IrValue, HashSet<BasicBlock>>();
            foreach (var instruction in block.GetPhiNodes())
            {
                var incomingBlocks = new HashSet<BasicBlock>();
                foreach (var arg in instruction.Arguments)
                {
                    if (arg is BasicBlock b)
                    {
                        incomingBlocks.Add(b);
                    }
                }

                if (instruction.Result is not null)
                {
                    incomingValues[instruction.Result] = incomingBlocks;
                }

                var expectedBlocks = new HashSet<BasicBlock>(block.Predecessors);
                if (!incomingBlocks.SetEquals(expectedBlocks))
                {
                    var missing = expectedBlocks.Except(incomingBlocks).Select(b => b.Label);
                    var extra = incomingBlocks.Except(expectedBlocks).Select(b => b.Label);

                    var msg = $"Phi 指令的入边基本块不完整";
                    if (missing.Any())
                    {
                        msg += $"，缺少: [{string.Join(", ", missing)}]";
                    }
                    if (extra.Any())
                    {
                        msg += $"，多余: [{string.Join(", ", extra)}]";
                    }

                    _errors.Add(new ValidationError("SSA_E003", msg,
                        function, block, instruction));
                }
            }
        }
    }

    private void ValidateOperandDominance(IrFunction function)
    {
        var domTree = new DominatorTree(function);
        var definitions = new Dictionary<IrValue, BasicBlock>();

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null)
                {
                    definitions[instruction.Result] = block;
                }
            }
        }

        foreach (var block in function.Blocks)
        {
            foreach (var phiInstruction in block.GetPhiNodes())
            {
                for (int i = 0; i < phiInstruction.Operands.Count && i < phiInstruction.Arguments.Count; i++)
                {
                    var operand = phiInstruction.Operands[i];
                    var incomingBlock = phiInstruction.Arguments[i] as BasicBlock;

                    if (!definitions.TryGetValue(operand, out var defBlock))
                    {
                        continue;
                    }

                    if (incomingBlock is not null && !domTree.Dominates(defBlock, incomingBlock))
                    {
                        _errors.Add(new ValidationError("SSA_E004",
                            $"Phi 操作数 '{operand}' 的定义块 '{defBlock.Label}' 不支配入边块 '{incomingBlock.Label}'",
                            function, block, phiInstruction));
                    }
                }
            }
        }
    }

    #endregion
}
