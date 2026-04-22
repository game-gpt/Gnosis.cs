namespace Gnosis.IR.Analysis;

public sealed class LivenessAnalyzer
{
    #region Properties

    public IrFunction Function { get; }

    public IReadOnlyDictionary<BasicBlock, HashSet<IrValue>> LiveIn => _liveIn;

    public IReadOnlyDictionary<BasicBlock, HashSet<IrValue>> LiveOut => _liveOut;

    public IReadOnlyDictionary<BasicBlock, HashSet<IrValue>> Defs => _defs;

    public IReadOnlyDictionary<BasicBlock, HashSet<IrValue>> Uses => _uses;

    #endregion

    #region Fields

    private readonly Dictionary<BasicBlock, HashSet<IrValue>> _liveIn = [];
    private readonly Dictionary<BasicBlock, HashSet<IrValue>> _liveOut = [];
    private readonly Dictionary<BasicBlock, HashSet<IrValue>> _defs = [];
    private readonly Dictionary<BasicBlock, HashSet<IrValue>> _uses = [];

    #endregion

    #region Constructors

    public LivenessAnalyzer(IrFunction function)
    {
        Function = function;
        Analyze();
    }

    #endregion

    #region Public Methods

    public bool IsLiveAt(IrValue value, BasicBlock block)
    {
        return _liveIn.TryGetValue(block, out var liveIn) && liveIn.Contains(value);
    }

    public bool IsLiveOut(IrValue value, BasicBlock block)
    {
        return _liveOut.TryGetValue(block, out var liveOut) && liveOut.Contains(value);
    }

    public IReadOnlySet<IrValue> GetLiveIn(BasicBlock block)
    {
        return _liveIn.GetValueOrDefault(block, new HashSet<IrValue>());
    }

    public IReadOnlySet<IrValue> GetLiveOut(BasicBlock block)
    {
        return _liveOut.GetValueOrDefault(block, new HashSet<IrValue>());
    }

    public HashSet<IrValue> GetLiveValuesAtInstruction(IrInstruction instruction)
    {
        var block = instruction.Parent;
        if (block is null)
        {
            return [];
        }

        var live = new HashSet<IrValue>(_liveOut.GetValueOrDefault(block, new HashSet<IrValue>()));

        for (int i = block.Instructions.Count - 1; i >= 0; i--)
        {
            var inst = block.Instructions[i];
            if (inst.Result is not null)
            {
                live.Remove(inst.Result);
            }
            foreach (var operand in inst.Operands)
            {
                live.Add(operand);
            }

            if (ReferenceEquals(inst, instruction))
            {
                break;
            }
        }

        return live;
    }

    public HashSet<IrValue> GetAllLiveValues()
    {
        var allLive = new HashSet<IrValue>();
        foreach (var (_, values) in _liveIn)
        {
            allLive.UnionWith(values);
        }
        return allLive;
    }

    public HashSet<IrValue> GetDeadValues()
    {
        var allDefined = new HashSet<IrValue>();
        var allUsed = new HashSet<IrValue>();

        foreach (var block in Function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null)
                {
                    allDefined.Add(instruction.Result);
                }
                foreach (var operand in instruction.Operands)
                {
                    allUsed.Add(operand);
                }
            }
        }

        allDefined.ExceptWith(allUsed);
        return allDefined;
    }

    #endregion

    #region Private Methods

    private void Analyze()
    {
        foreach (var block in Function.Blocks)
        {
            _defs[block] = ComputeDefs(block);
            _uses[block] = ComputeUses(block);
            _liveIn[block] = [];
            _liveOut[block] = [];
        }

        bool changed = true;
        while (changed)
        {
            changed = false;

            var postorder = ComputePostorder();
            foreach (var block in postorder)
            {
                var oldLiveOut = new HashSet<IrValue>(_liveOut[block]);

                _liveOut[block].Clear();
                foreach (var successor in block.Successors)
                {
                    _liveOut[block].UnionWith(_liveIn[successor]);
                }

                var newLiveIn = new HashSet<IrValue>(_uses[block]);
                var liveOutMinusDefs = new HashSet<IrValue>(_liveOut[block]);
                liveOutMinusDefs.ExceptWith(_defs[block]);
                newLiveIn.UnionWith(liveOutMinusDefs);

                if (!newLiveIn.SetEquals(_liveIn[block]) || !oldLiveOut.SetEquals(_liveOut[block]))
                {
                    changed = true;
                    _liveIn[block] = newLiveIn;
                }
            }
        }
    }

    private static HashSet<IrValue> ComputeDefs(BasicBlock block)
    {
        var defs = new HashSet<IrValue>();
        foreach (var instruction in block.Instructions)
        {
            if (instruction.Result is not null)
            {
                defs.Add(instruction.Result);
            }
        }
        return defs;
    }

    private static HashSet<IrValue> ComputeUses(BasicBlock block)
    {
        var uses = new HashSet<IrValue>();
        var defs = new HashSet<IrValue>();

        foreach (var instruction in block.Instructions)
        {
            foreach (var operand in instruction.Operands)
            {
                if (!defs.Contains(operand))
                {
                    uses.Add(operand);
                }
            }

            if (instruction.Result is not null)
            {
                defs.Add(instruction.Result);
            }
        }

        return uses;
    }

    private List<BasicBlock> ComputePostorder()
    {
        var order = new List<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        TraversePostorder(Function.EntryBlock, visited, order);
        return order;
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

    #endregion
}
