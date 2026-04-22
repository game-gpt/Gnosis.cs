using Gnosis.IR.Graph;

namespace Gnosis.IR.Analysis;

public sealed class DominatorTree
{
    #region Properties

    public IrFunction Function { get; }

    public BasicBlock Root { get; }

    public IReadOnlyDictionary<BasicBlock, BasicBlock?> ImmediateDominators => _immediateDominators;

    public IReadOnlyDictionary<BasicBlock, HashSet<BasicBlock>> DominanceFrontiers => _dominanceFrontiers;

    public IReadOnlyDictionary<BasicBlock, HashSet<BasicBlock>> DominatedBlocks => _dominatedBlocks;

    public IReadOnlyDictionary<BasicBlock, BasicBlock?> PostDominators => _postDominators;

    #endregion

    #region Fields

    private readonly Dictionary<BasicBlock, BasicBlock?> _immediateDominators = [];
    private readonly Dictionary<BasicBlock, HashSet<BasicBlock>> _dominanceFrontiers = [];
    private readonly Dictionary<BasicBlock, HashSet<BasicBlock>> _dominatedBlocks = [];
    private readonly Dictionary<BasicBlock, BasicBlock?> _postDominators = [];

    #endregion

    #region Constructors

    public DominatorTree(IrFunction function)
    {
        Function = function;
        Root = function.EntryBlock;
        ComputeImmediateDominators();
        ComputeDominatedBlocks();
        ComputeDominanceFrontiers();
        ComputePostDominators();
    }

    #endregion

    #region Public Methods

    public BasicBlock? GetImmediateDominator(BasicBlock block)
    {
        return _immediateDominators.TryGetValue(block, out var idom) ? idom : null;
    }

    public bool Dominates(BasicBlock dominator, BasicBlock block)
    {
        if (dominator == block)
        {
            return true;
        }

        var current = GetImmediateDominator(block);
        while (current is not null)
        {
            if (current == dominator)
            {
                return true;
            }
            current = GetImmediateDominator(current);
        }

        return false;
    }

    public bool StrictlyDominates(BasicBlock dominator, BasicBlock block)
    {
        return dominator != block && Dominates(dominator, block);
    }

    public IReadOnlySet<BasicBlock> GetDominanceFrontier(BasicBlock block)
    {
        return _dominanceFrontiers.GetValueOrDefault(block, new HashSet<BasicBlock>());
    }

    public IReadOnlySet<BasicBlock> GetDominatedBlocks(BasicBlock block)
    {
        return _dominatedBlocks.GetValueOrDefault(block, new HashSet<BasicBlock>());
    }

    public BasicBlock? FindLeastCommonAncestor(BasicBlock a, BasicBlock b)
    {
        var pathA = new HashSet<BasicBlock>();
        var current = a;
        while (current is not null)
        {
            pathA.Add(current);
            current = GetImmediateDominator(current);
        }

        current = b;
        while (current is not null)
        {
            if (pathA.Contains(current))
            {
                return current;
            }
            current = GetImmediateDominator(current);
        }

        return Root;
    }

    public IReadOnlyList<BasicBlock> GetPreorder()
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        TraversePreorder(Root, visited, order);
        return order;
    }

    public IReadOnlyList<BasicBlock> GetChildren(BasicBlock block)
    {
        var children = new List<BasicBlock>();
        foreach (var (b, idom) in _immediateDominators)
        {
            if (idom == block)
            {
                children.Add(b);
            }
        }
        return children;
    }

    #endregion

    #region Private Methods - Immediate Dominators (Cooper's Algorithm)

    private void ComputeImmediateDominators()
    {
        _immediateDominators[Root] = null;

        var rpo = ComputeReversePostorder();
        var rpoNumbers = new Dictionary<BasicBlock, int>();
        for (int i = 0; i < rpo.Count; i++)
        {
            rpoNumbers[rpo[i]] = i;
        }

        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (var block in rpo)
            {
                if (block == Root)
                {
                    continue;
                }

                var predecessors = block.Predecessors
                    .Where(p => _immediateDominators.ContainsKey(p))
                    .ToList();

                if (predecessors.Count == 0)
                {
                    continue;
                }

                BasicBlock? newIdom = predecessors[0];
                for (int i = 1; i < predecessors.Count; i++)
                {
                    newIdom = Intersect(predecessors[i], newIdom, rpoNumbers);
                }

                if (_immediateDominators.GetValueOrDefault(block) != newIdom)
                {
                    _immediateDominators[block] = newIdom;
                    changed = true;
                }
            }
        }
    }

    private BasicBlock? Intersect(BasicBlock? b1, BasicBlock? b2, Dictionary<BasicBlock, int> rpoNumbers)
    {
        var finger1 = b1;
        var finger2 = b2;

        while (finger1 != finger2)
        {
            while (finger1 is not null && finger2 is not null && rpoNumbers.GetValueOrDefault(finger1) > rpoNumbers.GetValueOrDefault(finger2))
            {
                finger1 = _immediateDominators.GetValueOrDefault(finger1);
            }

            while (finger2 is not null && finger1 is not null && rpoNumbers.GetValueOrDefault(finger2) > rpoNumbers.GetValueOrDefault(finger1))
            {
                finger2 = _immediateDominators.GetValueOrDefault(finger2);
            }
        }

        return finger1;
    }

    #endregion

    #region Private Methods - Dominated Blocks

    private void ComputeDominatedBlocks()
    {
        foreach (var block in Function.Blocks)
        {
            _dominatedBlocks[block] = [];
        }

        foreach (var block in Function.Blocks)
        {
            var current = GetImmediateDominator(block);
            while (current is not null)
            {
                _dominatedBlocks[current].Add(block);
                current = GetImmediateDominator(current);
            }
        }
    }

    #endregion

    #region Private Methods - Dominance Frontiers

    private void ComputeDominanceFrontiers()
    {
        foreach (var block in Function.Blocks)
        {
            _dominanceFrontiers[block] = [];
        }

        foreach (var block in Function.Blocks)
        {
            if (block.Predecessors.Count < 2)
            {
                continue;
            }

            foreach (var pred in block.Predecessors)
            {
                var runner = pred;
                while (runner is not null && runner != GetImmediateDominator(block))
                {
                    _dominanceFrontiers[runner].Add(block);
                    runner = GetImmediateDominator(runner);
                }
            }
        }
    }

    #endregion

    #region Private Methods - Post-Dominators

    private void ComputePostDominators()
    {
        var exitBlocks = Function.Blocks.Where(b => b.Successors.Count == 0).ToList();
        if (exitBlocks.Count == 0)
        {
            return;
        }

        var exitBlock = exitBlocks[0];
        _postDominators[exitBlock] = null;

        var reverseRpo = ComputeReverseReversePostorder(exitBlock);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in reverseRpo)
            {
                if (block == exitBlock)
                {
                    continue;
                }

                var successors = block.Successors
                    .Where(s => _postDominators.ContainsKey(s))
                    .ToList();

                if (successors.Count == 0)
                {
                    continue;
                }

                BasicBlock? newPdom = successors[0];
                for (int i = 1; i < successors.Count; i++)
                {
                    var current = successors[i];
                    while (newPdom is not null && current is not null && newPdom != current)
                    {
                        if (GetPostDominatorDepth(newPdom) < GetPostDominatorDepth(current))
                        {
                            newPdom = _postDominators.GetValueOrDefault(newPdom);
                        }
                        else
                        {
                            current = _postDominators.GetValueOrDefault(current);
                        }
                    }
                }

                if (_postDominators.GetValueOrDefault(block) != newPdom)
                {
                    _postDominators[block] = newPdom;
                    changed = true;
                }
            }
        }
    }

    private int GetPostDominatorDepth(BasicBlock? block)
    {
        int depth = 0;
        var current = block;
        while (current is not null)
        {
            depth++;
            current = _postDominators.GetValueOrDefault(current);
        }
        return depth;
    }

    #endregion

    #region Private Methods - Traversal

    private List<BasicBlock> ComputeReversePostorder()
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(Root);

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

    private List<BasicBlock> ComputeReverseReversePostorder(BasicBlock exitBlock)
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(exitBlock);

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
            for (int i = block.Predecessors.Count - 1; i >= 0; i--)
            {
                var pred = block.Predecessors[i];
                if (!visited.Contains(pred))
                {
                    stack.Push(pred);
                }
            }
        }

        order.Reverse();
        return order;
    }

    private static void TraversePreorder(BasicBlock block, HashSet<BasicBlock> visited, List<BasicBlock> order)
    {
        if (!visited.Add(block))
        {
            return;
        }

        order.Add(block);
        foreach (var successor in block.Successors)
        {
            TraversePreorder(successor, visited, order);
        }
    }

    #endregion
}
