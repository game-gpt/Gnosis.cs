using Gnosis.IR.Graph;

namespace Gnosis.IR.Transform;

public sealed class ConstantFolder
{
    #region Properties

    public IrModule Module { get; }

    public int FoldedCount { get; private set; }

    #endregion

    #region Fields

    private readonly Dictionary<IrValue, object?> _constantValues = [];

    #endregion

    #region Constructors

    public ConstantFolder(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Run()
    {
        FoldedCount = 0;
        bool changed = false;

        foreach (var function in Module.Functions)
        {
            if (FoldFunction(function))
            {
                changed = true;
            }
        }

        return changed;
    }

    public object? GetConstantValue(IrValue value)
    {
        return _constantValues.GetValueOrDefault(value);
    }

    public bool IsConstant(IrValue value)
    {
        return _constantValues.ContainsKey(value);
    }

    #endregion

    #region Private Methods

    private bool FoldFunction(IrFunction function)
    {
        bool changed = false;
        _constantValues.Clear();

        foreach (var block in function.Blocks)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instruction = block.Instructions[i];
                if (instruction.Result is null)
                {
                    continue;
                }

                var foldedValue = TryFoldInstruction(instruction);
                if (foldedValue is not null)
                {
                    _constantValues[instruction.Result] = foldedValue;
                }

                var simplified = TrySimplifyInstruction(instruction);
                if (simplified is not null && !ReferenceEquals(simplified, instruction))
                {
                    block.ReplaceInstruction(i, simplified);
                    changed = true;
                    FoldedCount++;
                }
            }
        }

        return changed;
    }

    private object? TryFoldInstruction(IrInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case IrOpcode.Add:
                return TryFoldBinary(instruction, (a, b) => a + b, (a, b) => a + b);

            case IrOpcode.Sub:
                return TryFoldBinary(instruction, (a, b) => a - b, (a, b) => a - b);

            case IrOpcode.Mul:
                return TryFoldBinary(instruction, (a, b) => a * b, (a, b) => a * b);

            case IrOpcode.Div:
                return TryFoldDiv(instruction);

            case IrOpcode.Mod:
                return TryFoldBinary(instruction, (a, b) => b != 0 ? a % b : null, (a, b) => b != 0 ? a % b : null);

            case IrOpcode.Neg:
                return TryFoldUnary(instruction, a => -a, a => -a);

            case IrOpcode.Equal:
                return TryFoldComparison(instruction, (a, b) => a == b, (a, b) => Math.Abs(a - b) < double.Epsilon);

            case IrOpcode.NotEqual:
                return TryFoldComparison(instruction, (a, b) => a != b, (a, b) => Math.Abs(a - b) >= double.Epsilon);

            case IrOpcode.Less:
                return TryFoldComparison(instruction, (a, b) => a < b, (a, b) => a < b);

            case IrOpcode.Greater:
                return TryFoldComparison(instruction, (a, b) => a > b, (a, b) => a > b);

            case IrOpcode.LessEqual:
                return TryFoldComparison(instruction, (a, b) => a <= b, (a, b) => a <= b);

            case IrOpcode.GreaterEqual:
                return TryFoldComparison(instruction, (a, b) => a >= b, (a, b) => a >= b);

            case IrOpcode.LogicalAnd:
                return TryFoldLogicalAnd(instruction);

            case IrOpcode.LogicalOr:
                return TryFoldLogicalOr(instruction);

            case IrOpcode.LogicalNot:
                return TryFoldLogicalNot(instruction);

            default:
                return null;
        }
    }

    private object? TryFoldBinary(IrInstruction instruction, Func<long, long, long?> intOp, Func<double, double, double?> floatOp)
    {
        if (instruction.Operands.Count < 2)
        {
            return null;
        }

        var leftVal = GetConstantValue(instruction.Operands[0]);
        var rightVal = GetConstantValue(instruction.Operands[1]);

        if (leftVal is null || rightVal is null)
        {
            return null;
        }

        if (leftVal is long l && rightVal is long r)
        {
            return intOp(l, r);
        }

        if (leftVal is double dl && rightVal is double dr)
        {
            return floatOp(dl, dr);
        }

        return null;
    }

    private object? TryFoldDiv(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2)
        {
            return null;
        }

        var leftVal = GetConstantValue(instruction.Operands[0]);
        var rightVal = GetConstantValue(instruction.Operands[1]);

        if (leftVal is null || rightVal is null)
        {
            return null;
        }

        if (leftVal is long l && rightVal is long r)
        {
            if (r == 0)
            {
                return null;
            }
            return l / r;
        }

        if (leftVal is double dl && rightVal is double dr)
        {
            if (Math.Abs(dr) < double.Epsilon)
            {
                return null;
            }
            return dl / dr;
        }

        return null;
    }

    private object? TryFoldUnary(IrInstruction instruction, Func<long, long?> intOp, Func<double, double?> floatOp)
    {
        if (instruction.Operands.Count < 1)
        {
            return null;
        }

        var operandVal = GetConstantValue(instruction.Operands[0]);
        if (operandVal is null)
        {
            return null;
        }

        if (operandVal is long l)
        {
            return intOp(l);
        }

        if (operandVal is double d)
        {
            return floatOp(d);
        }

        return null;
    }

    private object? TryFoldComparison(IrInstruction instruction, Func<long, long, bool> intOp, Func<double, double, bool> floatOp)
    {
        if (instruction.Operands.Count < 2)
        {
            return null;
        }

        var leftVal = GetConstantValue(instruction.Operands[0]);
        var rightVal = GetConstantValue(instruction.Operands[1]);

        if (leftVal is null || rightVal is null)
        {
            return null;
        }

        if (leftVal is long l && rightVal is long r)
        {
            return intOp(l, r);
        }

        if (leftVal is double dl && rightVal is double dr)
        {
            return floatOp(dl, dr);
        }

        return null;
    }

    private object? TryFoldLogicalAnd(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2)
        {
            return null;
        }

        var leftVal = GetConstantValue(instruction.Operands[0]);
        var rightVal = GetConstantValue(instruction.Operands[1]);

        if (leftVal is bool lb)
        {
            if (!lb)
            {
                return false;
            }
            if (rightVal is bool rb)
            {
                return rb;
            }
        }

        if (rightVal is bool rb2 && !rb2)
        {
            return false;
        }

        return null;
    }

    private object? TryFoldLogicalOr(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2)
        {
            return null;
        }

        var leftVal = GetConstantValue(instruction.Operands[0]);
        var rightVal = GetConstantValue(instruction.Operands[1]);

        if (leftVal is bool lb)
        {
            if (lb)
            {
                return true;
            }
            if (rightVal is bool rb)
            {
                return rb;
            }
        }

        if (rightVal is bool rb2 && rb2)
        {
            return true;
        }

        return null;
    }

    private object? TryFoldLogicalNot(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 1)
        {
            return null;
        }

        var operandVal = GetConstantValue(instruction.Operands[0]);
        if (operandVal is bool b)
        {
            return !b;
        }

        return null;
    }

    private IrInstruction? TrySimplifyInstruction(IrInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case IrOpcode.Add:
                return TrySimplifyAdd(instruction);

            case IrOpcode.Sub:
                return TrySimplifySub(instruction);

            case IrOpcode.Mul:
                return TrySimplifyMul(instruction);

            case IrOpcode.ConditionalBranch:
                return TrySimplifyConditionalBranch(instruction);

            default:
                return null;
        }
    }

    private IrInstruction? TrySimplifyAdd(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2 || instruction.Result is null)
        {
            return null;
        }

        var rightVal = GetConstantValue(instruction.Operands[1]);
        if (rightVal is long l && l == 0)
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        if (rightVal is double d && Math.Abs(d) < double.Epsilon)
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        return null;
    }

    private IrInstruction? TrySimplifySub(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2 || instruction.Result is null)
        {
            return null;
        }

        var rightVal = GetConstantValue(instruction.Operands[1]);
        if (rightVal is long l && l == 0)
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        if (instruction.Operands[0] == instruction.Operands[1])
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        return null;
    }

    private IrInstruction? TrySimplifyMul(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 2 || instruction.Result is null)
        {
            return null;
        }

        var rightVal = GetConstantValue(instruction.Operands[1]);
        if (rightVal is long l && l == 1)
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        if (rightVal is long l2 && l2 == 0)
        {
            return IrInstruction.Cast(instruction.Result, instruction.Operands[0]);
        }

        return null;
    }

    private IrInstruction? TrySimplifyConditionalBranch(IrInstruction instruction)
    {
        if (instruction.Operands.Count < 1 || instruction.Arguments.Count < 2)
        {
            return null;
        }

        var condVal = GetConstantValue(instruction.Operands[0]);
        if (condVal is bool b)
        {
            var target = b ? instruction.Arguments[0] : instruction.Arguments[1];
            if (target is BasicBlock targetBlock)
            {
                return IrInstruction.Branch(targetBlock);
            }
        }

        return null;
    }

    #endregion
}
