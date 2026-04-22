using Gnosis.IR.Graph;

namespace Gnosis.IR.Validation;

public sealed class ValidationError
{
    #region Properties

    public string Code { get; }

    public string Message { get; }

    public IrFunction? Function { get; }

    public BasicBlock? Block { get; }

    public IrInstruction? Instruction { get; }

    public Gnosis.Core.Diagnostic.SourceSpan? Span { get; }

    #endregion

    #region Constructors

    public ValidationError(string code, string message,
        IrFunction? function = null, BasicBlock? block = null,
        IrInstruction? instruction = null,
        Gnosis.Core.Diagnostic.SourceSpan? span = null)
    {
        Code = code;
        Message = message;
        Function = function;
        Block = block;
        Instruction = instruction;
        Span = span;
    }

    #endregion

    #region Public Methods

    public override string ToString()
    {
        var location = "";
        if (Function is not null)
        {
            location += $" in {Function.Name}";
        }
        if (Block is not null)
        {
            location += $" at {Block.Label}";
        }

        return $"[{Code}] {Message}{location}";
    }

    #endregion
}

public sealed class IrValidator
{
    #region Properties

    public Graph.IrModule Module { get; }

    public IReadOnlyList<ValidationError> Errors => _errors;

    public IReadOnlyList<ValidationError> Warnings => _warnings;

    public bool IsValid => _errors.Count == 0;

    #endregion

    #region Fields

    private readonly List<ValidationError> _errors = [];
    private readonly List<ValidationError> _warnings = [];

    #endregion

    #region Constructors

    public IrValidator(Graph.IrModule module)
    {
        Module = module;
    }

    #endregion

    #region Public Methods

    public bool Validate()
    {
        _errors.Clear();
        _warnings.Clear();

        ValidateModule();
        ValidateFunctions();
        ValidateControlFlow();
        ValidateInstructions();

        return IsValid;
    }

    #endregion

    #region Private Methods - Module Validation

    private void ValidateModule()
    {
        if (Module.Functions.Count == 0)
        {
            _warnings.Add(new ValidationError("IR_W001", "模块不包含任何函数"));
        }

        var functionNames = new HashSet<string>();
        foreach (var function in Module.Functions)
        {
            if (!functionNames.Add(function.Name))
            {
                _errors.Add(new ValidationError("IR_E001", $"函数名重复: {function.Name}", function));
            }
        }
    }

    #endregion

    #region Private Methods - Function Validation

    private void ValidateFunctions()
    {
        foreach (var function in Module.Functions)
        {
            ValidateFunction(function);
        }
    }

    private void ValidateFunction(Graph.IrFunction function)
    {
        if (function.Blocks.Count == 0)
        {
            _errors.Add(new ValidationError("IR_E002", $"函数 '{function.Name}' 不包含任何基本块", function));
            return;
        }

        foreach (var block in function.Blocks)
        {
            if (!block.IsTerminated)
            {
                _errors.Add(new ValidationError("IR_E003",
                    $"基本块 '{block.Label}' 未终止（缺少分支/返回指令）",
                    function, block));
            }
        }

        foreach (var block in function.Blocks)
        {
            if (block.Instructions.Count > 0)
            {
                var terminator = block.Instructions[^1];
                if (!terminator.IsTerminator() && block != function.Blocks[^1])
                {
                    _warnings.Add(new ValidationError("IR_W002",
                        $"基本块 '{block.Label}' 的最后一条指令不是终止指令",
                        function, block));
                }
            }
        }

        var definedValues = new HashSet<IrValue>();
        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Result is not null)
                {
                    if (!definedValues.Add(instruction.Result))
                    {
                        _errors.Add(new ValidationError("IR_E004",
                            $"值 '{instruction.Result}' 被多次定义",
                            function, block, instruction));
                    }
                }
            }
        }
    }

    #endregion

    #region Private Methods - Control Flow Validation

    private void ValidateControlFlow()
    {
        foreach (var function in Module.Functions)
        {
            ValidateControlFlowInFunction(function);
        }
    }

    private void ValidateControlFlowInFunction(Graph.IrFunction function)
    {
        var reachable = new HashSet<Graph.BasicBlock>();
        var stack = new Stack<Graph.BasicBlock>();
        stack.Push(function.EntryBlock);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (!reachable.Add(block))
            {
                continue;
            }

            foreach (var successor in block.Successors)
            {
                if (!function.Blocks.Contains(successor))
                {
                    _errors.Add(new ValidationError("IR_E005",
                        $"基本块 '{block.Label}' 的后继 '{successor.Label}' 不属于当前函数",
                        function, block));
                }
                else
                {
                    stack.Push(successor);
                }
            }
        }

        foreach (var block in function.Blocks)
        {
            if (!reachable.Contains(block))
            {
                _warnings.Add(new ValidationError("IR_W003",
                    $"基本块 '{block.Label}' 不可达",
                    function, block));
            }
        }

        foreach (var block in function.Blocks)
        {
            foreach (var pred in block.Predecessors)
            {
                if (!function.Blocks.Contains(pred))
                {
                    _errors.Add(new ValidationError("IR_E006",
                        $"基本块 '{block.Label}' 的前驱 '{pred.Label}' 不属于当前函数",
                        function, block));
                }
            }
        }
    }

    #endregion

    #region Private Methods - Instruction Validation

    private void ValidateInstructions()
    {
        foreach (var function in Module.Functions)
        {
            foreach (var block in function.Blocks)
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var instruction = block.Instructions[i];
                    ValidateInstruction(instruction, function, block, i);
                }
            }
        }
    }

    private void ValidateInstruction(Graph.IrInstruction instruction, Graph.IrFunction function, Graph.BasicBlock block, int index)
    {
        if (instruction.Opcode == Graph.IrOpcode.Phi)
        {
            if (index > 0)
            {
                var prevInstruction = block.Instructions[index - 1];
                if (prevInstruction.Opcode != Graph.IrOpcode.Phi)
                {
                    _errors.Add(new ValidationError("IR_E007",
                        $"Phi 指令不在基本块开头（索引 {index}）",
                        function, block, instruction));
                }
            }

            if (instruction.Arguments.Count != block.Predecessors.Count)
            {
                _errors.Add(new ValidationError("IR_E008",
                    $"Phi 指令的入边数量 ({instruction.Arguments.Count}) 与基本块前驱数量 ({block.Predecessors.Count}) 不匹配",
                    function, block, instruction));
            }
        }

        if (instruction.IsTerminator() && index != block.Instructions.Count - 1)
        {
            _errors.Add(new ValidationError("IR_E009",
                $"终止指令不在基本块末尾（索引 {index}）",
                function, block, instruction));
        }

        if (!instruction.IsTerminator() && index == block.Instructions.Count - 1 && block.Instructions.Count > 0)
        {
            _warnings.Add(new ValidationError("IR_W004",
                $"基本块 '{block.Label}' 的最后一条指令不是终止指令",
                function, block, instruction));
        }

        if (instruction.Opcode is Graph.IrOpcode.Branch or Graph.IrOpcode.ConditionalBranch)
        {
            if (instruction.Arguments.Count == 0)
            {
                _errors.Add(new ValidationError("IR_E010",
                    "分支指令缺少目标基本块",
                    function, block, instruction));
            }
        }

        if (instruction.Opcode == Graph.IrOpcode.ConditionalBranch)
        {
            if (instruction.Operands.Count < 1)
            {
                _errors.Add(new ValidationError("IR_E011",
                    "条件分支指令缺少条件操作数",
                    function, block, instruction));
            }
            else if (instruction.Operands[0].Type.Kind != Graph.IrTypeKind.Bool)
            {
                _errors.Add(new ValidationError("IR_E012",
                    $"条件分支的条件类型应为 bool，实际为 {instruction.Operands[0].Type}",
                    function, block, instruction));
            }
        }

        if (instruction.Opcode == Graph.IrOpcode.Return)
        {
            if (instruction.Operands.Count > 0)
            {
                var returnType = instruction.Operands[0].Type;
                if (function.ReturnType.Kind != Graph.IrTypeKind.Void && returnType != function.ReturnType)
                {
                    _errors.Add(new ValidationError("IR_E013",
                        $"返回值类型 {returnType} 与函数返回类型 {function.ReturnType} 不匹配",
                        function, block, instruction));
                }
            }
            else if (function.ReturnType.Kind != Graph.IrTypeKind.Void)
            {
                _errors.Add(new ValidationError("IR_E014",
                    $"函数返回类型为 {function.ReturnType}，但 return 指令无返回值",
                    function, block, instruction));
            }
        }
    }

    #endregion
}
