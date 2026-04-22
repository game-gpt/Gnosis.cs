namespace Gnosis.IR.Graph;

public sealed class SsaBuilder
{
    #region Fields

    private readonly IrFunction _function;
    private readonly Dictionary<string, Stack<IrValue>> _currentVersions = [];
    private readonly Dictionary<string, List<IrValue>> _allVersions = [];
    private readonly Dictionary<string, List<BasicBlock>> _defBlocks = [];

    #endregion

    #region Constructors

    public SsaBuilder(IrFunction function)
    {
        _function = function;
    }

    #endregion

    #region Public Methods

    public void Build()
    {
        CollectVariableDefinitions();
        InsertPhiFunctions();
        RenameVariables();
    }

    public IrValue ReadVariable(string name, BasicBlock block)
    {
        if (_currentVersions.TryGetValue(name, out var stack) && stack.Count > 0)
        {
            return stack.Peek();
        }

        return ReadVariableRecursive(name, block);
    }

    public void WriteVariable(string name, IrValue value, BasicBlock block)
    {
        if (!_currentVersions.ContainsKey(name))
        {
            _currentVersions[name] = new Stack<IrValue>();
        }
        _currentVersions[name].Push(value);

        if (!_allVersions.ContainsKey(name))
        {
            _allVersions[name] = new List<IrValue>();
        }
        _allVersions[name].Add(value);

        if (!_defBlocks.ContainsKey(name))
        {
            _defBlocks[name] = new List<BasicBlock>();
        }
        if (!_defBlocks[name].Contains(block))
        {
            _defBlocks[name].Add(block);
        }
    }

    #endregion

    #region Private Methods

    private void CollectVariableDefinitions()
    {
        foreach (var block in _function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null && instruction.Result.Name is not null)
                {
                    WriteVariable(instruction.Result.Name, instruction.Result, block);
                }
            }
        }

        _currentVersions.Clear();
    }

    private void InsertPhiFunctions()
    {
        var cfg = new ControlFlowGraph(_function);
        var domFrontiers = ComputeDominanceFrontiers(cfg);

        foreach (var (varName, defBlocks) in _defBlocks)
        {
            var workList = new Queue<BasicBlock>(defBlocks);
            var inserted = new HashSet<BasicBlock>();

            while (workList.Count > 0)
            {
                var block = workList.Dequeue();
                if (!domFrontiers.TryGetValue(block, out var frontier))
                {
                    continue;
                }

                foreach (var dfBlock in frontier)
                {
                    if (inserted.Add(dfBlock))
                    {
                        var phiResult = _function.CreateValue(IrType.Void, varName);
                        var incoming = dfBlock.Predecessors
                            .Select(pred => (ReadVariable(varName, pred), pred))
                            .ToList();

                        var phi = IrInstruction.Phi(phiResult, incoming);
                        dfBlock.Insert(0, phi);

                        if (!_defBlocks[varName].Contains(dfBlock))
                        {
                            _defBlocks[varName].Add(dfBlock);
                            workList.Enqueue(dfBlock);
                        }
                    }
                }
            }
        }
    }

    private void RenameVariables()
    {
        var counter = new Dictionary<string, int>();
        var stack = new Dictionary<string, Stack<IrValue>>();

        RenameBlock(_function.EntryBlock, counter, stack);
    }

    private void RenameBlock(BasicBlock block, Dictionary<string, int> counter, Dictionary<string, Stack<IrValue>> stack)
    {
        var pushedCounts = new Dictionary<string, int>();

        foreach (var instruction in block.Instructions.ToList())
        {
            if (instruction.Opcode != IrOpcode.Phi)
            {
                for (int i = 0; i < instruction.Operands.Count; i++)
                {
                    var operand = instruction.Operands[i];
                    if (operand.Name is not null && stack.TryGetValue(operand.Name, out var opStack) && opStack.Count > 0)
                    {
                        var currentVersion = opStack.Peek();
                        instruction.ReplaceOperand(operand, currentVersion);
                    }
                }
            }

            if (instruction.Result is not null && instruction.Result.Name is not null)
            {
                var name = instruction.Result.Name;
                if (!counter.ContainsKey(name))
                {
                    counter[name] = 0;
                }
                counter[name]++;

                var newName = $"{name}_{counter[name]}";
                instruction.Result.Name = newName;

                if (!stack.ContainsKey(name))
                {
                    stack[name] = new Stack<IrValue>();
                }
                stack[name].Push(instruction.Result);

                if (!pushedCounts.ContainsKey(name))
                {
                    pushedCounts[name] = 0;
                }
                pushedCounts[name]++;
            }
        }

        foreach (var successor in block.Successors)
        {
            foreach (var phi in successor.GetPhiNodes())
            {
                if (phi.Result?.Name is not null)
                {
                    var baseName = GetBaseName(phi.Result.Name);
                    if (stack.TryGetValue(baseName, out var opStack) && opStack.Count > 0)
                    {
                        var incoming = phi.Operands.ToList();
                        incoming.Add(opStack.Peek());
                    }
                }
            }
        }

        var cfg = new ControlFlowGraph(_function);
        var idom = cfg.GetImmediateDominator(block);
        if (idom is not null)
        {
            foreach (var dominated in _function.Blocks.Where(b => cfg.GetImmediateDominator(b) == block))
            {
                RenameBlock(dominated, counter, stack);
            }
        }

        foreach (var (name, count) in pushedCounts)
        {
            for (int i = 0; i < count; i++)
            {
                stack[name].Pop();
            }
        }
    }

    private IrValue ReadVariableRecursive(string name, BasicBlock block)
    {
        if (block.Predecessors.Count == 0)
        {
            return _function.CreateValue(IrType.Void, $"{name}_undef");
        }

        if (block.Predecessors.Count == 1)
        {
            return ReadVariable(name, block.Predecessors[0]);
        }

        var phiResult = _function.CreateValue(IrType.Void, name);
        var incoming = block.Predecessors
            .Select(pred => (ReadVariable(name, pred), pred))
            .ToList();

        var phi = IrInstruction.Phi(phiResult, incoming);
        block.Insert(0, phi);

        WriteVariable(name, phiResult, block);
        return phiResult;
    }

    private static Dictionary<BasicBlock, HashSet<BasicBlock>> ComputeDominanceFrontiers(ControlFlowGraph cfg)
    {
        var result = new Dictionary<BasicBlock, HashSet<BasicBlock>>();

        foreach (var block in cfg.Blocks)
        {
            result[block] = new HashSet<BasicBlock>();
        }

        foreach (var block in cfg.Blocks)
        {
            if (block.Predecessors.Count < 2)
            {
                continue;
            }

            foreach (var pred in block.Predecessors)
            {
                var runner = pred;
                while (runner is not null && runner != cfg.GetImmediateDominator(block))
                {
                    if (!result.ContainsKey(runner))
                    {
                        result[runner] = new HashSet<BasicBlock>();
                    }
                    result[runner].Add(block);
                    runner = cfg.GetImmediateDominator(runner);
                }
            }
        }

        return result;
    }

    private static string GetBaseName(string ssaName)
    {
        var lastUnderscore = ssaName.LastIndexOf('_');
        if (lastUnderscore > 0 && int.TryParse(ssaName[(lastUnderscore + 1)..], out _))
        {
            return ssaName[..lastUnderscore];
        }
        return ssaName;
    }

    #endregion
}
