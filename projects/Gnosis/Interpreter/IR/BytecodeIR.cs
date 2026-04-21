using Gnosis.Compiler;

namespace Gnosis.Interpreter.IR;

public sealed class BytecodeUnit
{
    #region Properties

    public string ModuleName { get; }

    public IReadOnlyList<object> Constants { get; }

    public IReadOnlyList<BytecodeFunction> Functions { get; }

    public IReadOnlyList<string> Imports { get; }

    public IReadOnlyList<string> Exports { get; }

    public IReadOnlyList<(int Offset, SourceSpan? Span)> SourceMap { get; }

    #endregion

    #region Constructors

    public BytecodeUnit(
        string moduleName,
        IReadOnlyList<object> constants,
        IReadOnlyList<BytecodeFunction> functions,
        IReadOnlyList<string> imports,
        IReadOnlyList<string> exports,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        ModuleName = moduleName;
        Constants = constants;
        Functions = functions;
        Imports = imports;
        Exports = exports;
        SourceMap = sourceMap;
    }

    #endregion
}

public sealed class BytecodeFunction
{
    #region Properties

    public string Name { get; }

    public int ParameterCount { get; }

    public int LocalCount { get; }

    public IReadOnlyList<BytecodeInstruction> Instructions { get; }

    #endregion

    #region Constructors

    public BytecodeFunction(string name, int parameterCount, int localCount, IReadOnlyList<BytecodeInstruction> instructions)
    {
        Name = name;
        ParameterCount = parameterCount;
        LocalCount = localCount;
        Instructions = instructions;
    }

    #endregion
}

public sealed class BytecodeInstruction
{
    #region Properties

    public OpCode OpCode { get; }

    public long Operand { get; }

    #endregion

    #region Constructors

    public BytecodeInstruction(OpCode opCode, long operand = 0)
    {
        OpCode = opCode;
        Operand = operand;
    }

    #endregion

    #region Public Methods

    public override string ToString()
    {
        return Operand != 0 ? $"{OpCode} {Operand}" : OpCode.ToString();
    }

    #endregion
}
