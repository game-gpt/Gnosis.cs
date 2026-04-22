using Gnosis.IR.Graph;

namespace Gnosis.IR.Transform;

public sealed class LoopUnroller
{
    #region Properties

    public IrModule Module { get; }

    public int UnrolledCount { get; private set; }

    public int MaxUnrollFactor { get; set; } = 8;

    #endregion

    #region Constructors

    public LoopUnroller(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Run()
    {
        UnrolledCount = 0;
        bool changed = false;

        foreach (var function in Module.Functions)
        {
            if (UnrollLoopsInFunction(function))
            {
                changed = true;
            }
        }

        return changed;
    }

    #endregion

    #region Private Methods

    private bool UnrollLoopsInFunction(IrFunction function)
    {
        var loopDetector = new Analysis.LoopDetector(function);
        if (!loopDetector.HasLoops)
        {
            return false;
        }

        bool changed = false;

        foreach (var loop in loopDetector.Loops.ToList())
        {
            if (ShouldUnroll(loop, function))
            {
                if (TryUnrollLoop(loop, function))
                {
                    changed = true;
                    UnrolledCount++;
                }
            }
        }

        return changed;
    }

    private bool ShouldUnroll(Analysis.LoopInfo loop, IrFunction function)
    {
        int instructionCount = 0;
        foreach (var block in loop.Blocks)
        {
            instructionCount += block.Instructions.Count;
        }

        if (instructionCount > MaxUnrollFactor * 10)
        {
            return false;
        }

        if (loop.IsInnerLoop)
        {
            return true;
        }

        if (instructionCount <= 20)
        {
            return true;
        }

        return false;
    }

    private bool TryUnrollLoop(Analysis.LoopInfo loop, IrFunction function)
    {
        var header = loop.Header;
        var backEdge = loop.BackEdgeSource;

        if (!IsSimpleLoop(loop))
        {
            return false;
        }

        var loopBlocks = loop.Blocks.OrderBy(b => function.GetBlockIndex(b)).ToList();

        var preHeader = FindOrCreatePreHeader(header, function);
        if (preHeader is null)
        {
            return false;
        }

        var exitBlocks = loop.Exits;
        if (exitBlocks.Count != 1)
        {
            return false;
        }

        var exitBlock = exitBlocks[0];

        var unrolledBody = function.CreateBlock($"{header.Label}_unrolled");

        foreach (var block in loopBlocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Opcode == IrOpcode.Phi)
                {
                    continue;
                }

                if (instruction.IsTerminator())
                {
                    continue;
                }

                var cloned = CloneInstructionForUnroll(instruction);
                if (cloned is not null)
                {
                    unrolledBody.Append(cloned);
                }
            }
        }

        unrolledBody.Append(IrInstruction.Branch(exitBlock));

        preHeader.Append(IrInstruction.Branch(unrolledBody));

        function.RemoveBlock(header);

        return true;
    }

    private bool IsSimpleLoop(Analysis.LoopInfo loop)
    {
        if (loop.Blocks.Count > 3)
        {
            return false;
        }

        if (loop.Exits.Count != 1)
        {
            return false;
        }

        return true;
    }

    private BasicBlock? FindOrCreatePreHeader(BasicBlock header, IrFunction function)
    {
        BasicBlock? preHeader = null;

        foreach (var pred in header.Predecessors)
        {
            if (!header.Predecessors.Contains(pred))
            {
                continue;
            }

            bool isInsideLoop = false;
            foreach (var succ in pred.Successors)
            {
                if (succ == header)
                {
                    isInsideLoop = true;
                    break;
                }
            }

            if (!isInsideLoop)
            {
                preHeader = pred;
                break;
            }
        }

        if (preHeader is null)
        {
            preHeader = function.CreateBlock($"{header.Label}_preheader");
            preHeader.Append(IrInstruction.Branch(header));
        }

        return preHeader;
    }

    private static IrInstruction? CloneInstructionForUnroll(IrInstruction instruction)
    {
        return new IrInstruction(
            instruction.Opcode,
            instruction.Result,
            instruction.Operands.ToList(),
            instruction.Arguments.ToList(),
            instruction.Span);
    }

    #endregion
}
