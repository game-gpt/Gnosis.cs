namespace Gnosis.IR.Analysis;

public enum AliasResult
{
    MustAlias,
    MayAlias,
    NoAlias
}

public sealed class AliasAnalyzer
{
    #region Properties

    public IrFunction Function { get; }

    #endregion

    #region Fields

    private readonly Dictionary<IrValue, HashSet<IrValue>> _aliasSets = [];

    #endregion

    #region Constructors

    public AliasAnalyzer(IrFunction function)
    {
        Function = function;
        Analyze();
    }

    #endregion

    #region Public Methods

    public AliasResult Alias(IrValue a, IrValue b)
    {
        if (ReferenceEquals(a, b))
        {
            return AliasResult.MustAlias;
        }

        if (a.Type.Kind != b.Type.Kind)
        {
            return AliasResult.NoAlias;
        }

        if (IsUniqueAllocation(a) && IsUniqueAllocation(b))
        {
            return AliasResult.NoAlias;
        }

        if (_aliasSets.TryGetValue(a, out var aliases) && aliases.Contains(b))
        {
            return AliasResult.MayAlias;
        }

        if (IsReadOnly(a) || IsReadOnly(b))
        {
            return AliasResult.NoAlias;
        }

        return AliasResult.MayAlias;
    }

    public IReadOnlySet<IrValue> GetAliasSet(IrValue value)
    {
        return _aliasSets.GetValueOrDefault(value, new HashSet<IrValue>());
    }

    public bool MayAliasWith(IrValue a, IrValue b)
    {
        return Alias(a, b) != AliasResult.NoAlias;
    }

    #endregion

    #region Private Methods

    private void Analyze()
    {
        var allocas = new Dictionary<IrValue, IrInstruction>();
        var stores = new List<IrInstruction>();
        var loads = new List<IrInstruction>();

        foreach (var block in Function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                switch (instruction.Opcode)
                {
                    case IrOpcode.Alloca:
                        if (instruction.Result is not null)
                        {
                            allocas[instruction.Result] = instruction;
                        }
                        break;
                    case IrOpcode.Store:
                        stores.Add(instruction);
                        break;
                    case IrOpcode.Load:
                        loads.Add(instruction);
                        break;
                }
            }
        }

        foreach (var allica in allocas)
        {
            _aliasSets[allica.Key] = new HashSet<IrValue> { allica.Key };
        }

        foreach (var store in stores)
        {
            if (store.Operands.Count < 2)
            {
                continue;
            }

            var pointer = store.Operands[1];
            if (!_aliasSets.ContainsKey(pointer))
            {
                _aliasSets[pointer] = new HashSet<IrValue>();
            }

            _aliasSets[pointer].Add(pointer);

            foreach (var otherPointer in GetPointersToSameType(pointer))
            {
                _aliasSets[pointer].Add(otherPointer);
                if (_aliasSets.ContainsKey(otherPointer))
                {
                    _aliasSets[otherPointer].Add(pointer);
                }
            }
        }
    }

    private bool IsUniqueAllocation(IrValue value)
    {
        foreach (var block in Function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Opcode == IrOpcode.Alloca && instruction.Result is not null)
                {
                    if (ReferenceEquals(instruction.Result, value))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsReadOnly(IrValue value)
    {
        foreach (var block in Function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Opcode == IrOpcode.Store && instruction.Operands.Count >= 2)
                {
                    if (ReferenceEquals(instruction.Operands[1], value))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private IEnumerable<IrValue> GetPointersToSameType(IrValue pointer)
    {
        var pointerElementType = GetPointedType(pointer);

        foreach (var block in Function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null && !ReferenceEquals(instruction.Result, pointer))
                {
                    var resultElementType = GetPointedType(instruction.Result);
                    if (resultElementType == pointerElementType && resultElementType is not null)
                    {
                        yield return instruction.Result;
                    }
                }
            }
        }
    }

    private static IrType? GetPointedType(IrValue value)
    {
        if (value.Type.Kind == IrTypeKind.Reference && value.Type.GenericArgs.Count > 0)
        {
            return value.Type.GenericArgs[0];
        }

        return null;
    }

    #endregion
}
