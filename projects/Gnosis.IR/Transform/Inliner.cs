using Gnosis.IR.Graph;

namespace Gnosis.IR.Transform;

public sealed class Inliner
{
    #region Properties

    public IrModule Module { get; }

    public int InlinedCount { get; private set; }

    public int MaxInlineSize { get; set; } = 50;

    public int MaxInlineDepth { get; set; } = 3;

    #endregion

    #region Constructors

    public Inliner(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Run()
    {
        InlinedCount = 0;
        bool changed = false;

        foreach (var function in Module.Functions.ToList())
        {
            if (InlineCallsInFunction(function))
            {
                changed = true;
            }
        }

        return changed;
    }

    #endregion

    #region Private Methods

    private bool InlineCallsInFunction(IrFunction function)
    {
        bool changed = false;

        foreach (var block in function.Blocks.ToList())
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instruction = block.Instructions[i];

                if (instruction.Opcode is not (IrOpcode.Call or IrOpcode.CallNative))
                {
                    continue;
                }

                if (instruction.Arguments.Count == 0 || instruction.Arguments[0] is not string calleeName)
                {
                    continue;
                }

                var callee = Module.FindFunction(calleeName);
                if (callee is null)
                {
                    continue;
                }

                if (!ShouldInline(callee))
                {
                    continue;
                }

                if (TryInlineCall(function, block, i, callee, instruction))
                {
                    changed = true;
                    InlinedCount++;
                }
            }
        }

        return changed;
    }

    private bool ShouldInline(IrFunction callee)
    {
        int instructionCount = 0;
        foreach (var block in callee.Blocks)
        {
            instructionCount += block.Instructions.Count;
        }

        if (instructionCount > MaxInlineSize)
        {
            return false;
        }

        if (IsRecursive(callee))
        {
            return false;
        }

        return true;
    }

    private bool IsRecursive(IrFunction function)
    {
        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Opcode is IrOpcode.Call or IrOpcode.CallNative)
                {
                    if (instruction.Arguments.Count > 0 && instruction.Arguments[0] is string name && name == function.Name)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryInlineCall(IrFunction caller, BasicBlock callBlock, int callIndex,
        IrFunction callee, IrInstruction callInstruction)
    {
        var calleeBlocks = callee.Blocks.ToList();
        if (calleeBlocks.Count == 0)
        {
            return false;
        }

        var valueMap = new Dictionary<IrValue, IrValue>();

        for (int i = 0; i < callee.Parameters.Count && i < callInstruction.Operands.Count; i++)
        {
            var paramValue = callee.CreateParameter(i);
            valueMap[paramValue] = callInstruction.Operands[i];
        }

        var blockMap = new Dictionary<BasicBlock, BasicBlock>();
        foreach (var calleeBlock in calleeBlocks)
        {
            var newBlock = caller.CreateBlock($"{calleeBlock.Label}_inline");
            blockMap[calleeBlock] = newBlock;
        }

        foreach (var calleeBlock in calleeBlocks)
        {
            var newBlock = blockMap[calleeBlock];

            foreach (var instruction in calleeBlock.Instructions)
            {
                if (instruction.Opcode == IrOpcode.Return)
                {
                    if (instruction.Operands.Count > 0 && callInstruction.Result is not null)
                    {
                        var returnValue = MapValue(instruction.Operands[0], valueMap);
                        var assign = IrInstruction.Cast(callInstruction.Result, returnValue);
                        newBlock.Append(assign);
                    }

                    if (callIndex + 1 < callBlock.Instructions.Count)
                    {
                        var continueBlock = caller.CreateBlock($"{callBlock.Label}_continue");
                        newBlock.Append(IrInstruction.Branch(continueBlock));

                        var remainingInstructions = callBlock.Instructions
                            .Skip(callIndex + 1)
                            .ToList();

                        foreach (var remaining in remainingInstructions)
                        {
                            continueBlock.Append(CloneInstruction(remaining, valueMap, blockMap));
                        }

                        for (int j = callBlock.Instructions.Count - 1; j > callIndex; j--)
                        {
                            callBlock.RemoveAt(j);
                        }
                    }
                    else
                    {
                        var continueBlock = blockMap.GetValueOrDefault(calleeBlocks[0], newBlock);
                        if (calleeBlock != calleeBlocks[0])
                        {
                            newBlock.Append(IrInstruction.Branch(continueBlock));
                        }
                    }

                    continue;
                }

                var cloned = CloneInstruction(instruction, valueMap, blockMap);
                if (cloned is not null)
                {
                    newBlock.Append(cloned);
                }
            }
        }

        callBlock.RemoveAt(callIndex);

        var entryInlineBlock = blockMap[callee.EntryBlock];
        callBlock.Append(IrInstruction.Branch(entryInlineBlock));

        return true;
    }

    private IrValue MapValue(IrValue value, Dictionary<IrValue, IrValue> valueMap)
    {
        return valueMap.TryGetValue(value, out var mapped) ? mapped : value;
    }

    private IrInstruction? CloneInstruction(IrInstruction instruction,
        Dictionary<IrValue, IrValue> valueMap,
        Dictionary<BasicBlock, BasicBlock> blockMap)
    {
        var operands = instruction.Operands.Select(o => MapValue(o, valueMap)).ToList();
        var arguments = instruction.Arguments.Select(a => a is BasicBlock b ? (object)blockMap.GetValueOrDefault(b, b) : a).ToList();

        return new IrInstruction(instruction.Opcode, instruction.Result, operands, arguments, instruction.Span);
    }

    #endregion
}
