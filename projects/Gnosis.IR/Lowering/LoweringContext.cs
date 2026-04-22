using Gnosis.IR.Graph;

namespace Gnosis.IR.Lowering;

public sealed class LoweringContext
{
    #region Properties

    public IrModule Module { get; }

    public IrFunction CurrentFunction { get; private set; } = null!;

    public BasicBlock CurrentBlock { get; private set; } = null!;

    public IReadOnlyDictionary<string, IrValue> NamedValues => _namedValues;

    #endregion

    #region Fields

    private readonly Dictionary<string, IrValue> _namedValues = [];
    private readonly Dictionary<string, IrType> _namedTypes = [];
    private readonly Dictionary<string, BasicBlock> _labels = [];

    #endregion

    #region Constructors

    public LoweringContext(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public void SetFunction(IrFunction function)
    {
        CurrentFunction = function;
    }

    public void SetBlock(BasicBlock block)
    {
        CurrentBlock = block;
    }

    public BasicBlock CreateBlock(string? label = null)
    {
        return CurrentFunction.CreateBlock(label);
    }

    public IrValue CreateValue(IrType type, string? name = null)
    {
        return CurrentFunction.CreateValue(type, name);
    }

    public void Emit(IrInstruction instruction)
    {
        CurrentBlock.Append(instruction);
    }

    public void BindValue(string name, IrValue value)
    {
        _namedValues[name] = value;
    }

    public IrValue? LookupValue(string name)
    {
        return _namedValues.GetValueOrDefault(name);
    }

    public void BindType(string name, IrType type)
    {
        _namedTypes[name] = type;
    }

    public IrType? LookupType(string name)
    {
        return _namedTypes.GetValueOrDefault(name);
    }

    public void BindLabel(string name, BasicBlock block)
    {
        _labels[name] = block;
    }

    public BasicBlock? LookupLabel(string name)
    {
        return _labels.GetValueOrDefault(name);
    }

    public void PushScope()
    {
    }

    public void PopScope()
    {
    }

    public void Reset()
    {
        _namedValues.Clear();
        _namedTypes.Clear();
        _labels.Clear();
    }

    #endregion
}
