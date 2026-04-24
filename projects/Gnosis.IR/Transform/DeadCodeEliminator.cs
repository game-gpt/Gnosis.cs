using Gnosis.IR.Graph;

namespace Gnosis.IR.Transform;

public sealed class DeadCodeEliminator : IOptimizationPass
{
    #region Properties

    public IrModule Module { get; }

    public int EliminatedInstructions { get; private set; }

    public int EliminatedBlocks { get; private set; }

    public string Name => "DeadCodeEliminator";

    public bool Changed { get; private set; }

    #endregion

    #region Constructors

    public DeadCodeEliminator(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Run()
    {
        EliminatedInstructions = 0;
        EliminatedBlocks = 0;
        Changed = false;

        if (EliminateUnreachableBlocks())
        {
            Changed = true;
        }

        if (EliminateDeadInstructions())
        {
            Changed = true;
        }

        return Changed;
    }

    public void Reset()
    {
        Changed = false;
        EliminatedInstructions = 0;
        EliminatedBlocks = 0;
    }

    #endregion

    #region Private Methods - Unreachable Block Elimination

    private bool EliminateUnreachableBlocks()
    {
        bool changed = false;

        foreach (var function in Module.Functions)
        {
            changed |= EliminateUnreachableBlocksInFunction(function);
        }

        return changed;
    }

    private bool EliminateUnreachableBlocksInFunction(IrFunction function)
    {
        var reachable = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(function.EntryBlock);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (!reachable.Add(block))
            {
                continue;
            }

            foreach (var successor in block.Successors)
            {
                stack.Push(successor);
            }
        }

        var unreachableBlocks = function.Blocks.Where(b => !reachable.Contains(b)).ToList();
        if (unreachableBlocks.Count == 0)
        {
            return false;
        }

        foreach (var block in unreachableBlocks)
        {
            EliminatedBlocks++;
            EliminatedInstructions += block.Instructions.Count;
            function.RemoveBlock(block);
        }

        return true;
    }

    #endregion

    #region Private Methods - Dead Instruction Elimination

    private bool EliminateDeadInstructions()
    {
        bool changed = false;

        foreach (var function in Module.Functions)
        {
            changed |= EliminateDeadInstructionsInFunction(function);
        }

        return changed;
    }

    private bool EliminateDeadInstructionsInFunction(IrFunction function)
    {
        var usedValues = CollectUsedValues(function);
        bool changed = false;

        foreach (var block in function.Blocks)
        {
            for (int i = block.Instructions.Count - 1; i >= 0; i--)
            {
                var instruction = block.Instructions[i];

                if (instruction.IsTerminator())
                {
                    continue;
                }

                if (instruction.Result is null)
                {
                    if (IsSideEffectFree(instruction))
                    {
                        block.RemoveAt(i);
                        EliminatedInstructions++;
                        changed = true;
                    }
                    continue;
                }

                if (!usedValues.Contains(instruction.Result) && IsRemovable(instruction))
                {
                    block.RemoveAt(i);
                    EliminatedInstructions++;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static HashSet<IrValue> CollectUsedValues(IrFunction function)
    {
        var used = new HashSet<IrValue>();

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                foreach (var operand in instruction.Operands)
                {
                    used.Add(operand);
                }

                foreach (var arg in instruction.Arguments)
                {
                    if (arg is BasicBlock)
                    {
                        continue;
                    }
                }
            }
        }

        return used;
    }

    private static bool IsSideEffectFree(IrInstruction instruction)
    {
        return instruction.Opcode is
            IrOpcode.Nop or
            IrOpcode.Cast or
            IrOpcode.Bitcast or
            IrOpcode.Add or IrOpcode.Sub or IrOpcode.Mul or
            IrOpcode.Div or IrOpcode.Mod or IrOpcode.Neg or
            IrOpcode.Shl or IrOpcode.Shr or
            IrOpcode.BitAnd or IrOpcode.BitOr or IrOpcode.BitXor or IrOpcode.BitNot or
            IrOpcode.Equal or IrOpcode.NotEqual or
            IrOpcode.Less or IrOpcode.Greater or IrOpcode.LessEqual or IrOpcode.GreaterEqual or
            IrOpcode.LogicalAnd or IrOpcode.LogicalOr or IrOpcode.LogicalNot or
            IrOpcode.ConstructVector or IrOpcode.ExtractElement or IrOpcode.Swizzle or
            IrOpcode.ConstructArray or IrOpcode.ArrayGet or IrOpcode.ArrayLength or
            IrOpcode.IsNull or IrOpcode.IsType or IrOpcode.TypeOf or
            IrOpcode.Phi;
    }

    private static bool IsRemovable(IrInstruction instruction)
    {
        if (instruction.Opcode == IrOpcode.Store)
        {
            return false;
        }

        if (instruction.Opcode == IrOpcode.Call || instruction.Opcode == IrOpcode.CallNative)
        {
            return false;
        }

        return IsSideEffectFree(instruction);
    }

    #endregion
}
