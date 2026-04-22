namespace Gnosis.IR.Graph;

public sealed class BasicBlock
{
    #region Properties

    public string Label { get; }

    public int Id { get; }

    public IReadOnlyList<IrInstruction> Instructions => _instructions;

    public IReadOnlyList<BasicBlock> Predecessors => _predecessors;

    public IReadOnlyList<BasicBlock> Successors => _successors;

    public IrFunction? Parent { get; internal set; }

    public bool IsSealed { get; private set; }

    public bool IsEmpty => _instructions.Count == 0;

    public bool IsTerminated => _instructions.Count > 0 && _instructions[^1].IsTerminator();

    #endregion

    #region Fields

    private readonly List<IrInstruction> _instructions = [];
    private readonly List<BasicBlock> _predecessors = [];
    private readonly List<BasicBlock> _successors = [];

    #endregion

    #region Constructors

    public BasicBlock(string label, int id)
    {
        Label = label;
        Id = id;
    }

    #endregion

    #region Internal Methods - Instruction Management

    internal void Append(IrInstruction instruction)
    {
        if (IsTerminated)
        {
            throw new InvalidOperationException($"基本块 '{Label}' 已终止，无法追加指令");
        }

        instruction.Parent = this;
        _instructions.Add(instruction);

        if (instruction.IsBranch())
        {
            foreach (var successor in instruction.GetSuccessorBlocks())
            {
                AddSuccessor(successor);
            }
        }
    }

    internal void Insert(int index, IrInstruction instruction)
    {
        instruction.Parent = this;
        _instructions.Insert(index, instruction);
    }

    internal void RemoveAt(int index)
    {
        _instructions[index].Parent = null;
        _instructions.RemoveAt(index);
    }

    internal void ReplaceInstruction(int index, IrInstruction instruction)
    {
        _instructions[index].Parent = null;
        instruction.Parent = this;
        _instructions[index] = instruction;
    }

    #endregion

    #region Internal Methods - Graph Management

    internal void AddPredecessor(BasicBlock block)
    {
        if (!_predecessors.Contains(block))
        {
            _predecessors.Add(block);
        }
    }

    internal void AddSuccessor(BasicBlock block)
    {
        if (!_successors.Contains(block))
        {
            _successors.Add(block);
            block.AddPredecessor(this);
        }
    }

    internal void RemovePredecessor(BasicBlock block)
    {
        _predecessors.Remove(block);
    }

    internal void RemoveSuccessor(BasicBlock block)
    {
        _successors.Remove(block);
        block.RemovePredecessor(this);
    }

    internal void Seal()
    {
        IsSealed = true;
    }

    #endregion

    #region Public Methods

    public IrInstruction? GetTerminator()
    {
        return IsTerminated ? _instructions[^1] : null;
    }

    public IEnumerable<IrValue> GetUsedValues()
    {
        var used = new HashSet<IrValue>();
        foreach (var instruction in _instructions)
        {
            foreach (var operand in instruction.Operands)
            {
                used.Add(operand);
            }
        }
        return used;
    }

    public IEnumerable<IrValue> GetDefinedValues()
    {
        var defined = new HashSet<IrValue>();
        foreach (var instruction in _instructions)
        {
            if (instruction.Result is not null)
            {
                defined.Add(instruction.Result);
            }
        }
        return defined;
    }

    public IEnumerable<IrInstruction> GetPhiNodes()
    {
        return _instructions.Where(i => i.Opcode == IrOpcode.Phi);
    }

    public IEnumerable<IrInstruction> GetNonPhiNodes()
    {
        return _instructions.Where(i => i.Opcode != IrOpcode.Phi);
    }

    public override string ToString()
    {
        return Label;
    }

    #endregion
}
