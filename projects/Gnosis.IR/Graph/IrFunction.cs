namespace Gnosis.IR.Graph;

public sealed class IrFunction
{
    #region Properties

    public string Name { get; }

    public IrType ReturnType { get; }

    public IReadOnlyList<(string Name, IrType Type)> Parameters { get; }

    public IReadOnlyList<BasicBlock> Blocks => _blocks;

    public BasicBlock EntryBlock => _blocks[0];

    public IrModule? Parent { get; internal set; }

    public IReadOnlyList<string> Attributes { get; }

    #endregion

    #region Fields

    private readonly List<BasicBlock> _blocks = [];
    private int _nextBlockId;
    private int _nextValueId;

    #endregion

    #region Constructors

    public IrFunction(string name, IrType returnType,
        IReadOnlyList<(string Name, IrType Type)>? parameters = null,
        IReadOnlyList<string>? attributes = null)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters ?? [];
        Attributes = attributes ?? [];
    }

    #endregion

    #region Public Methods - Block Management

    public BasicBlock CreateBlock(string? label = null)
    {
        var block = new BasicBlock(label ?? $"bb{_nextBlockId}", _nextBlockId);
        _nextBlockId++;
        block.Parent = this;
        _blocks.Add(block);
        return block;
    }

    public void InsertBlock(int index, BasicBlock block)
    {
        block.Parent = this;
        _blocks.Insert(index, block);
    }

    public void RemoveBlock(BasicBlock block)
    {
        foreach (var succ in block.Successors.ToList())
        {
            block.RemoveSuccessor(succ);
        }
        foreach (var pred in block.Predecessors.ToList())
        {
            pred.RemoveSuccessor(block);
        }
        block.Parent = null;
        _blocks.Remove(block);
    }

    #endregion

    #region Public Methods - Value Management

    public IrValue CreateValue(IrType type, string? name = null)
    {
        var value = new IrValue(_nextValueId, type, name);
        _nextValueId++;
        return value;
    }

    public IrValue CreateParameter(int index)
    {
        var (name, type) = Parameters[index];
        var value = new IrValue(_nextValueId, type, name);
        _nextValueId++;
        return value;
    }

    #endregion

    #region Public Methods - Query

    public IEnumerable<IrValue> GetAllValues()
    {
        var values = new HashSet<IrValue>();
        foreach (var block in _blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null)
                {
                    values.Add(instruction.Result);
                }
                foreach (var operand in instruction.Operands)
                {
                    values.Add(operand);
                }
            }
        }
        return values;
    }

    public IEnumerable<IrInstruction> GetAllInstructions()
    {
        foreach (var block in _blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                yield return instruction;
            }
        }
    }

    public BasicBlock? FindBlock(string label)
    {
        return _blocks.Find(b => b.Label == label);
    }

    public int GetBlockIndex(BasicBlock block)
    {
        return _blocks.IndexOf(block);
    }

    #endregion

    #region Public Methods - Validation

    public bool Validate()
    {
        if (_blocks.Count == 0)
        {
            return false;
        }

        foreach (var block in _blocks)
        {
            if (!block.IsTerminated)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Public Methods - Text IR

    public string ToIrText()
    {
        var writer = new StringWriter();
        WriteIrText(writer);
        return writer.ToString();
    }

    public void WriteIrText(TextWriter writer)
    {
        var attrs = Attributes.Count > 0 ? $" [{string.Join(", ", Attributes)}]" : "";
        var @params = string.Join(", ", Parameters.Select(p => $"{p.Name}: {p.Type}"));
        writer.WriteLine($"fn {Name}({@params}) -> {ReturnType}{attrs} {{");

        foreach (var block in _blocks)
        {
            writer.WriteLine($"  {block.Label}:");
            foreach (var instruction in block.Instructions)
            {
                writer.WriteLine($"    {instruction}");
            }
        }

        writer.WriteLine("}");
    }

    #endregion

    public override string ToString()
    {
        return $"fn {Name}({string.Join(", ", Parameters.Select(p => p.Type))}) -> {ReturnType}";
    }
}
