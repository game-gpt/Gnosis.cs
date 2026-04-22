namespace Gnosis.IR.Analysis;

public sealed class LoopInfo
{
    #region Properties

    public BasicBlock Header { get; }

    public BasicBlock BackEdgeSource { get; }

    public IReadOnlySet<BasicBlock> Blocks { get; }

    public IReadOnlyList<BasicBlock> Exits { get; }

    public int Depth { get; }

    public bool IsInnerLoop => Depth > 1;

    public bool IsSingleBlock => Blocks.Count == 1;

    public int BlockCount => Blocks.Count;

    #endregion

    #region Constructors

    public LoopInfo(BasicBlock header, BasicBlock backEdgeSource,
        IReadOnlySet<BasicBlock> blocks, IReadOnlyList<BasicBlock> exits, int depth)
    {
        Header = header;
        BackEdgeSource = backEdgeSource;
        Blocks = blocks;
        Exits = exits;
        Depth = depth;
    }

    #endregion

    #region Public Methods

    public bool Contains(BasicBlock block)
    {
        return Blocks.Contains(block);
    }

    public bool IsLoopExit(BasicBlock block)
    {
        return Exits.Contains(block);
    }

    public override string ToString()
    {
        return $"Loop(Header={Header}, Depth={Depth}, Blocks={BlockCount})";
    }

    #endregion
}

public sealed class LoopDetector
{
    #region Properties

    public IrFunction Function { get; }

    public IReadOnlyList<LoopInfo> Loops => _loops;

    public IReadOnlyDictionary<BasicBlock, int> LoopDepth => _loopDepth;

    public bool HasLoops => _loops.Count > 0;

    #endregion

    #region Fields

    private readonly List<LoopInfo> _loops = [];
    private readonly Dictionary<BasicBlock, int> _loopDepth = [];

    #endregion

    #region Constructors

    public LoopDetector(IrFunction function)
    {
        Function = function;
        Detect();
    }

    #endregion

    #region Public Methods

    public bool IsInLoop(BasicBlock block)
    {
        return _loopDepth.GetValueOrDefault(block) > 0;
    }

    public bool IsLoopHeader(BasicBlock block)
    {
        return _loops.Any(l => l.Header == block);
    }

    public LoopInfo? GetInnermostLoop(BasicBlock block)
    {
        LoopInfo? innermost = null;
        foreach (var loop in _loops)
        {
            if (loop.Contains(block))
            {
                if (innermost is null || loop.Depth > innermost.Depth)
                {
                    innermost = loop;
                }
            }
        }
        return innermost;
    }

    public IReadOnlyList<LoopInfo> GetOuterLoops()
    {
        return _loops.Where(l => l.Depth == 1).ToList();
    }

    public IReadOnlyList<LoopInfo> GetLoopsContaining(BasicBlock block)
    {
        return _loops.Where(l => l.Contains(block)).ToList();
    }

    #endregion

    #region Private Methods

    private void Detect()
    {
        var domTree = new DominatorTree(Function);

        foreach (var block in Function.Blocks)
        {
            _loopDepth[block] = 0;
        }

        foreach (var block in Function.Blocks)
        {
            foreach (var successor in block.Successors)
            {
                if (domTree.Dominates(successor, block))
                {
                    var loopBlocks = ComputeNaturalLoop(successor, block);
                    var loopExits = ComputeLoopExits(loopBlocks);
                    var loop = new LoopInfo(successor, block, loopBlocks, loopExits, 0);
                    _loops.Add(loop);
                }
            }
        }

        ComputeLoopNesting();
    }

    private HashSet<BasicBlock> ComputeNaturalLoop(BasicBlock header, BasicBlock backEdgeSource)
    {
        var loopBlocks = new HashSet<BasicBlock> { header };
        var stack = new Stack<BasicBlock>();
        stack.Push(backEdgeSource);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (loopBlocks.Contains(block))
            {
                continue;
            }

            loopBlocks.Add(block);
            foreach (var pred in block.Predecessors)
            {
                if (!loopBlocks.Contains(pred))
                {
                    stack.Push(pred);
                }
            }
        }

        return loopBlocks;
    }

    private List<BasicBlock> ComputeLoopExits(HashSet<BasicBlock> loopBlocks)
    {
        var exits = new List<BasicBlock>();
        foreach (var block in loopBlocks)
        {
            foreach (var successor in block.Successors)
            {
                if (!loopBlocks.Contains(successor))
                {
                    exits.Add(successor);
                }
            }
        }
        return exits;
    }

    private void ComputeLoopNesting()
    {
        for (int i = 0; i < _loops.Count; i++)
        {
            for (int j = 0; j < _loops.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (_loops[i].Blocks.IsProperSupersetOf(_loops[j].Blocks))
                {
                    _loops[j] = new LoopInfo(
                        _loops[j].Header,
                        _loops[j].BackEdgeSource,
                        _loops[j].Blocks,
                        _loops[j].Exits,
                        _loops[j].Depth + 1);
                }
            }
        }

        foreach (var loop in _loops)
        {
            foreach (var block in loop.Blocks)
            {
                _loopDepth[block] = Math.Max(_loopDepth[block], loop.Depth + 1);
            }
        }
    }

    #endregion
}
