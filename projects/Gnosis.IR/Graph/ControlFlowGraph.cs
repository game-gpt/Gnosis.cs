using Gnosis.IR.Instruction;

namespace Gnosis.IR.Graph;

public sealed class ControlFlowGraph
{
    #region Properties

    public IrFunction Function { get; }

    public IReadOnlyList<BasicBlock> Blocks => Function.Blocks;

    public BasicBlock EntryBlock => Function.EntryBlock;

    public int BlockCount => Function.Blocks.Count;

    public int EdgeCount { get; private set; }

    #endregion

    #region Constructors

    public ControlFlowGraph(IrFunction function)
    {
        Function = function;
        EdgeCount = CountEdges();
    }

    #endregion

    #region Public Methods - Construction from Bytecode

    public static ControlFlowGraph FromBytecodeFunction(BytecodeFunction bytecodeFunc)
    {
        var function = new IrFunction(bytecodeFunc.Name, IrType.Void);
        var builder = new CfgBuilder(function);
        builder.BuildFromBytecode(bytecodeFunc);
        return new ControlFlowGraph(function);
    }

    #endregion

    #region Public Methods - Analysis

    public IEnumerable<BasicBlock> GetExitBlocks()
    {
        return Blocks.Where(b => b.Successors.Count == 0);
    }

    public IEnumerable<BasicBlock> GetReachableBlocks()
    {
        var visited = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(EntryBlock);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (!visited.Add(block))
            {
                continue;
            }

            foreach (var successor in block.Successors)
            {
                stack.Push(successor);
            }
        }

        return visited;
    }

    public IEnumerable<BasicBlock> GetUnreachableBlocks()
    {
        var reachable = new HashSet<BasicBlock>(GetReachableBlocks());
        return Blocks.Where(b => !reachable.Contains(b));
    }

    public bool IsReachable(BasicBlock block)
    {
        return GetReachableBlocks().Contains(block);
    }

    public IReadOnlyList<BasicBlock> ReversePostorder()
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(EntryBlock);

        while (stack.Count > 0)
        {
            var block = stack.Peek();
            if (visited.Contains(block))
            {
                stack.Pop();
                order.Add(block);
                continue;
            }

            visited.Add(block);
            for (int i = block.Successors.Count - 1; i >= 0; i--)
            {
                var successor = block.Successors[i];
                if (!visited.Contains(successor))
                {
                    stack.Push(successor);
                }
            }
        }

        order.Reverse();
        return order;
    }

    public IReadOnlyList<BasicBlock> Postorder()
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        TraversePostorder(EntryBlock, visited, order);
        return order;
    }

    public IReadOnlyList<HashSet<BasicBlock>> ComputeNaturalLoops()
    {
        var loops = new List<HashSet<BasicBlock>>();
        var visited = new HashSet<BasicBlock>();

        foreach (var block in Blocks)
        {
            foreach (var successor in block.Successors)
            {
                if (IsAncestor(successor, block, visited))
                {
                    var loop = ComputeNaturalLoop(successor, block);
                    loops.Add(loop);
                }
            }
        }

        return loops;
    }

    public IReadOnlyList<BasicBlock> ComputeDominators()
    {
        var doms = new Dictionary<BasicBlock, HashSet<BasicBlock>>();
        var allBlocks = new HashSet<BasicBlock>(Blocks);
        var rpo = ReversePostorder();

        foreach (var block in rpo)
        {
            doms[block] = block == EntryBlock
                ? new HashSet<BasicBlock> { block }
                : new HashSet<BasicBlock>(allBlocks);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in rpo)
            {
                if (block == EntryBlock)
                {
                    continue;
                }

                var newDom = new HashSet<BasicBlock>(allBlocks);
                foreach (var pred in block.Predecessors)
                {
                    if (doms.TryGetValue(pred, out var predDom))
                    {
                        newDom.IntersectWith(predDom);
                    }
                }
                newDom.Add(block);

                if (!newDom.SetEquals(doms[block]))
                {
                    doms[block] = newDom;
                    changed = true;
                }
            }
        }

        return rpo;
    }

    public BasicBlock? GetImmediateDominator(BasicBlock block)
    {
        if (block == EntryBlock)
        {
            return null;
        }

        var doms = ComputeDominatorsForBlock(block);
        doms.Remove(block);

        BasicBlock? idom = null;
        foreach (var dom in doms)
        {
            if (idom == null || dom.Predecessors.Count > idom.Predecessors.Count)
            {
                idom = dom;
            }
        }

        return idom;
    }

    #endregion

    #region Private Methods

    private int CountEdges()
    {
        int count = 0;
        foreach (var block in Blocks)
        {
            count += block.Successors.Count;
        }
        return count;
    }

    private void TraversePostorder(BasicBlock block, HashSet<BasicBlock> visited, List<BasicBlock> order)
    {
        if (!visited.Add(block))
        {
            return;
        }

        foreach (var successor in block.Successors)
        {
            TraversePostorder(successor, visited, order);
        }

        order.Add(block);
    }

    private bool IsAncestor(BasicBlock potentialAncestor, BasicBlock block, HashSet<BasicBlock> visited)
    {
        visited.Clear();
        var stack = new Stack<BasicBlock>();
        stack.Push(EntryBlock);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == block)
            {
                return true;
            }
            if (!visited.Add(current))
            {
                continue;
            }
            if (current == potentialAncestor)
            {
                continue;
            }
            foreach (var succ in current.Successors)
            {
                stack.Push(succ);
            }
        }

        return false;
    }

    private HashSet<BasicBlock> ComputeNaturalLoop(BasicBlock header, BasicBlock backEdgeSource)
    {
        var loop = new HashSet<BasicBlock> { header };
        var stack = new Stack<BasicBlock>();
        stack.Push(backEdgeSource);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (loop.Contains(block))
            {
                continue;
            }

            loop.Add(block);
            foreach (var pred in block.Predecessors)
            {
                if (!loop.Contains(pred))
                {
                    stack.Push(pred);
                }
            }
        }

        return loop;
    }

    private HashSet<BasicBlock> ComputeDominatorsForBlock(BasicBlock target)
    {
        var allBlocks = new HashSet<BasicBlock>(Blocks);
        var doms = new Dictionary<BasicBlock, HashSet<BasicBlock>>();
        var rpo = ReversePostorder();

        foreach (var block in rpo)
        {
            doms[block] = block == EntryBlock
                ? new HashSet<BasicBlock> { block }
                : new HashSet<BasicBlock>(allBlocks);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in rpo)
            {
                if (block == EntryBlock)
                {
                    continue;
                }

                var newDom = new HashSet<BasicBlock>(allBlocks);
                foreach (var pred in block.Predecessors)
                {
                    if (doms.TryGetValue(pred, out var predDom))
                    {
                        newDom.IntersectWith(predDom);
                    }
                }
                newDom.Add(block);

                if (!newDom.SetEquals(doms[block]))
                {
                    doms[block] = newDom;
                    changed = true;
                }
            }
        }

        return doms.GetValueOrDefault(target, new HashSet<BasicBlock>());
    }

    #endregion

    #region Nested Types

    private sealed class CfgBuilder(IrFunction function)
    {
        private readonly IrFunction _function = function;
        private readonly Dictionary<int, BasicBlock> _blockStarts = [];
        private readonly Dictionary<int, string> _labels = [];

        public void BuildFromBytecode(BytecodeFunction bytecodeFunc)
        {
            IdentifyBlockBoundaries(bytecodeFunc);
            BuildBlocks(bytecodeFunc);
            ConnectBlocks();
        }

        private void IdentifyBlockBoundaries(BytecodeFunction bytecodeFunc)
        {
            for (int i = 0; i < bytecodeFunc.Instructions.Count; i++)
            {
                var inst = bytecodeFunc.Instructions[i];
                if (IsBranchOrReturn(inst.OpCode))
                {
                    if (i + 1 < bytecodeFunc.Instructions.Count)
                    {
                        _blockStarts[i + 1] = _function.CreateBlock($"L{i + 1}");
                    }
                }

                if (IsBranchTarget(inst.OpCode, inst.Operand))
                {
                    var targetOffset = (int)inst.Operand;
                    if (!_blockStarts.ContainsKey(targetOffset))
                    {
                        _blockStarts[targetOffset] = _function.CreateBlock($"L{targetOffset}");
                    }
                }
            }

            if (_function.Blocks.Count == 0)
            {
                _function.CreateBlock("entry");
            }
        }

        private void BuildBlocks(BytecodeFunction bytecodeFunc)
        {
            var currentBlock = _function.EntryBlock;
            for (int i = 0; i < bytecodeFunc.Instructions.Count; i++)
            {
                if (_blockStarts.TryGetValue(i, out var block) && block != currentBlock)
                {
                    currentBlock = block;
                }

                var inst = bytecodeFunc.Instructions[i];
                var irInst = ConvertInstruction(inst);
                if (irInst is not null)
                {
                    currentBlock.Append(irInst);
                }
            }
        }

        private void ConnectBlocks()
        {
            for (int i = 0; i < _function.Blocks.Count; i++)
            {
                var block = _function.Blocks[i];
                if (!block.IsTerminated && i + 1 < _function.Blocks.Count)
                {
                    var nextBlock = _function.Blocks[i + 1];
                    block.Append(IrInstruction.Branch(nextBlock));
                }
            }
        }

        private static bool IsBranchOrReturn(OpCode opCode)
        {
            return opCode is OpCode.Jump or OpCode.JumpIfTrue or OpCode.JumpIfFalse or OpCode.Return;
        }

        private static bool IsBranchTarget(OpCode opCode, long operand)
        {
            return opCode is OpCode.Jump or OpCode.JumpIfTrue or OpCode.JumpIfFalse && operand >= 0;
        }

        private static IrInstruction? ConvertInstruction(BytecodeInstruction inst)
        {
            return inst.OpCode switch
            {
                OpCode.Return => IrInstruction.Return(),
                OpCode.Halt => IrInstruction.Return(),
                OpCode.Nop => null,
                _ => null
            };
        }
    }

    #endregion
}
