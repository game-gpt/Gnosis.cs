using Gnosis.IR.Graph;

namespace Gnosis.IR.Validation;

public sealed class TypeChecker
{
    #region Properties

    public IrModule Module { get; }

    public IReadOnlyList<ValidationError> Errors => _errors;

    public bool IsValid => _errors.Count == 0;

    #endregion

    #region Fields

    private readonly List<ValidationError> _errors = [];
    private readonly Dictionary<IrValue, IrType> _valueTypes = [];

    #endregion

    #region Constructors

    public TypeChecker(IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Check()
    {
        _errors.Clear();
        _valueTypes.Clear();

        foreach (var function in Module.Functions)
        {
            CheckFunction(function);
        }

        return IsValid;
    }

    #endregion

    #region Private Methods

    private void CheckFunction(IrFunction function)
    {
        for (int i = 0; i < function.Parameters.Count; i++)
        {
            var (name, type) = function.Parameters[i];
            var paramValue = function.CreateParameter(i);
            _valueTypes[paramValue] = type;
        }

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                CheckInstruction(instruction, function, block);
            }
        }
    }

    private void CheckInstruction(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        switch (instruction.Opcode)
        {
            case IrOpcode.Add or IrOpcode.Sub or IrOpcode.Mul or IrOpcode.Div or IrOpcode.Mod:
                CheckBinaryArithmetic(instruction, function, block);
                break;

            case IrOpcode.Neg:
                CheckUnaryArithmetic(instruction, function, block);
                break;

            case IrOpcode.Equal or IrOpcode.NotEqual:
                CheckComparison(instruction, function, block, requireSameType: true);
                break;

            case IrOpcode.Less or IrOpcode.Greater or IrOpcode.LessEqual or IrOpcode.GreaterEqual:
                CheckOrderedComparison(instruction, function, block);
                break;

            case IrOpcode.LogicalAnd or IrOpcode.LogicalOr:
                CheckLogicalBinary(instruction, function, block);
                break;

            case IrOpcode.LogicalNot:
                CheckLogicalUnary(instruction, function, block);
                break;

            case IrOpcode.ConditionalBranch:
                CheckConditionalBranch(instruction, function, block);
                break;

            case IrOpcode.Call or IrOpcode.CallNative:
                CheckCall(instruction, function, block);
                break;

            case IrOpcode.Load:
                CheckLoad(instruction, function, block);
                break;

            case IrOpcode.Store:
                CheckStore(instruction, function, block);
                break;

            case IrOpcode.Alloca:
                CheckAlloca(instruction, function, block);
                break;

            case IrOpcode.Cast:
                CheckCast(instruction, function, block);
                break;

            case IrOpcode.Phi:
                CheckPhi(instruction, function, block);
                break;

            case IrOpcode.ConstructVector:
                CheckConstructVector(instruction, function, block);
                break;

            case IrOpcode.ExtractElement:
                CheckExtractElement(instruction, function, block);
                break;

            case IrOpcode.Swizzle:
                CheckSwizzle(instruction, function, block);
                break;

            case IrOpcode.NewObject:
                CheckNewObject(instruction, function, block);
                break;

            case IrOpcode.GetField or IrOpcode.SetField or IrOpcode.LoadField or IrOpcode.StoreField:
                CheckFieldAccess(instruction, function, block);
                break;
        }

        if (instruction.Result is not null)
        {
            _valueTypes[instruction.Result] = InferResultType(instruction);
        }
    }

    private void CheckBinaryArithmetic(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E001", "二元算术指令需要 2 个操作数", function, block, instruction));
            return;
        }

        var leftType = GetOperandType(instruction.Operands[0]);
        var rightType = GetOperandType(instruction.Operands[1]);

        if (!leftType.IsNumeric() && !leftType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E002",
                $"算术指令的左操作数类型 {leftType} 不是数值类型",
                function, block, instruction));
        }

        if (!rightType.IsNumeric() && !rightType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E003",
                $"算术指令的右操作数类型 {rightType} 不是数值类型",
                function, block, instruction));
        }

        if (leftType != rightType && !leftType.IsVector() && !rightType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E004",
                $"算术指令的操作数类型不匹配: {leftType} vs {rightType}",
                function, block, instruction));
        }
    }

    private void CheckUnaryArithmetic(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E005", "一元算术指令需要 1 个操作数", function, block, instruction));
            return;
        }

        var operandType = GetOperandType(instruction.Operands[0]);
        if (!operandType.IsNumeric() && !operandType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E006",
                $"取反指令的操作数类型 {operandType} 不是数值类型",
                function, block, instruction));
        }
    }

    private void CheckComparison(IrInstruction instruction, IrFunction function, BasicBlock block, bool requireSameType)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E007", "比较指令需要 2 个操作数", function, block, instruction));
            return;
        }

        var leftType = GetOperandType(instruction.Operands[0]);
        var rightType = GetOperandType(instruction.Operands[1]);

        if (requireSameType && leftType != rightType)
        {
            _errors.Add(new ValidationError("TC_E008",
                $"比较指令的操作数类型不匹配: {leftType} vs {rightType}",
                function, block, instruction));
        }
    }

    private void CheckOrderedComparison(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E009", "有序比较指令需要 2 个操作数", function, block, instruction));
            return;
        }

        var leftType = GetOperandType(instruction.Operands[0]);
        var rightType = GetOperandType(instruction.Operands[1]);

        if (!leftType.IsNumeric() && !leftType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E010",
                $"有序比较指令的左操作数类型 {leftType} 不是数值类型",
                function, block, instruction));
        }

        if (leftType != rightType)
        {
            _errors.Add(new ValidationError("TC_E011",
                $"有序比较指令的操作数类型不匹配: {leftType} vs {rightType}",
                function, block, instruction));
        }
    }

    private void CheckLogicalBinary(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E012", "逻辑二元指令需要 2 个操作数", function, block, instruction));
            return;
        }

        var leftType = GetOperandType(instruction.Operands[0]);
        var rightType = GetOperandType(instruction.Operands[1]);

        if (leftType.Kind != IrTypeKind.Bool)
        {
            _errors.Add(new ValidationError("TC_E013",
                $"逻辑指令的左操作数类型 {leftType} 不是 bool",
                function, block, instruction));
        }

        if (rightType.Kind != IrTypeKind.Bool)
        {
            _errors.Add(new ValidationError("TC_E014",
                $"逻辑指令的右操作数类型 {rightType} 不是 bool",
                function, block, instruction));
        }
    }

    private void CheckLogicalUnary(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E015", "逻辑一元指令需要 1 个操作数", function, block, instruction));
            return;
        }

        var operandType = GetOperandType(instruction.Operands[0]);
        if (operandType.Kind != IrTypeKind.Bool)
        {
            _errors.Add(new ValidationError("TC_E016",
                $"逻辑非指令的操作数类型 {operandType} 不是 bool",
                function, block, instruction));
        }
    }

    private void CheckConditionalBranch(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E017", "条件分支指令需要条件操作数", function, block, instruction));
            return;
        }

        var condType = GetOperandType(instruction.Operands[0]);
        if (condType.Kind != IrTypeKind.Bool)
        {
            _errors.Add(new ValidationError("TC_E018",
                $"条件分支的条件类型 {condType} 不是 bool",
                function, block, instruction));
        }
    }

    private void CheckCall(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Arguments.Count == 0 || instruction.Arguments[0] is not string calleeName)
        {
            _errors.Add(new ValidationError("TC_E019", "调用指令缺少函数名", function, block, instruction));
            return;
        }

        var callee = Module.FindFunction(calleeName);
        if (callee is not null && instruction.Operands.Count != callee.Parameters.Count)
        {
            _errors.Add(new ValidationError("TC_E020",
                $"调用 '{calleeName}' 的参数数量不匹配: 期望 {callee.Parameters.Count}，实际 {instruction.Operands.Count}",
                function, block, instruction));
        }
    }

    private void CheckLoad(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E021", "Load 指令需要指针操作数", function, block, instruction));
        }
    }

    private void CheckStore(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E022", "Store 指令需要值和指针操作数", function, block, instruction));
        }
    }

    private void CheckAlloca(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Result is null)
        {
            _errors.Add(new ValidationError("TC_E023", "Alloca 指令需要结果值", function, block, instruction));
        }
    }

    private void CheckCast(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E024", "Cast 指令需要操作数", function, block, instruction));
        }
    }

    private void CheckPhi(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Result is null)
        {
            _errors.Add(new ValidationError("TC_E025", "Phi 指令需要结果值", function, block, instruction));
            return;
        }

        foreach (var operand in instruction.Operands)
        {
            var operandType = GetOperandType(operand);
            if (operandType != instruction.Result.Type && operandType.Kind != IrTypeKind.Void)
            {
                _errors.Add(new ValidationError("TC_E026",
                    $"Phi 指令的入边类型 {operandType} 与结果类型 {instruction.Result.Type} 不匹配",
                    function, block, instruction));
            }
        }
    }

    private void CheckConstructVector(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count == 0)
        {
            _errors.Add(new ValidationError("TC_E027", "ConstructVector 指令需要至少 1 个操作数", function, block, instruction));
            return;
        }

        var elementType = GetOperandType(instruction.Operands[0]);
        for (int i = 1; i < instruction.Operands.Count; i++)
        {
            var operandType = GetOperandType(instruction.Operands[i]);
            if (operandType != elementType)
            {
                _errors.Add(new ValidationError("TC_E028",
                    $"ConstructVector 的元素类型不一致: {elementType} vs {operandType}",
                    function, block, instruction));
            }
        }
    }

    private void CheckExtractElement(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 2)
        {
            _errors.Add(new ValidationError("TC_E029", "ExtractElement 指令需要向量和索引操作数", function, block, instruction));
            return;
        }

        var vectorType = GetOperandType(instruction.Operands[0]);
        if (!vectorType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E030",
                $"ExtractElement 的第一个操作数类型 {vectorType} 不是向量",
                function, block, instruction));
        }
    }

    private void CheckSwizzle(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Operands.Count < 1)
        {
            _errors.Add(new ValidationError("TC_E031", "Swizzle 指令需要向量操作数", function, block, instruction));
            return;
        }

        var objectType = GetOperandType(instruction.Operands[0]);
        if (!objectType.IsVector())
        {
            _errors.Add(new ValidationError("TC_E032",
                $"Swizzle 的操作数类型 {objectType} 不是向量",
                function, block, instruction));
        }
    }

    private void CheckNewObject(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Arguments.Count == 0 || instruction.Arguments[0] is not string)
        {
            _errors.Add(new ValidationError("TC_E033", "NewObject 指令缺少类型名", function, block, instruction));
        }
    }

    private void CheckFieldAccess(IrInstruction instruction, IrFunction function, BasicBlock block)
    {
        if (instruction.Arguments.Count == 0 || instruction.Arguments[0] is not string)
        {
            _errors.Add(new ValidationError("TC_E034", "字段访问指令缺少字段名", function, block, instruction));
        }
    }

    private IrType GetOperandType(IrValue value)
    {
        return _valueTypes.TryGetValue(value, out var type) ? type : value.Type;
    }

    private static IrType InferResultType(IrInstruction instruction)
    {
        if (instruction.Result is not null)
        {
            return instruction.Result.Type;
        }

        return instruction.Opcode switch
        {
            IrOpcode.Equal or IrOpcode.NotEqual or
            IrOpcode.Less or IrOpcode.Greater or IrOpcode.LessEqual or IrOpcode.GreaterEqual or
            IrOpcode.LogicalAnd or IrOpcode.LogicalOr or IrOpcode.LogicalNot or
            IrOpcode.IsNull or IrOpcode.IsType => IrType.Bool,

            IrOpcode.Call or IrOpcode.CallNative => instruction.Result?.Type ?? IrType.Void,

            IrOpcode.Alloca => IrType.ReferenceTo(
                instruction.Arguments.Count > 0 && instruction.Arguments[0] is IrType t ? t : IrType.Void),

            IrOpcode.TypeOf => IrType.String,

            _ => instruction.Result?.Type ?? IrType.Void
        };
    }

    #endregion
}
