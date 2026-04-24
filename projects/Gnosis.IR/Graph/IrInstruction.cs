using Gnosis.Core.Diagnostic;

namespace Gnosis.IR.Graph;

public enum IrOpcode
{
    Nop,

    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Neg,

    Shl,
    Shr,
    BitAnd,
    BitOr,
    BitXor,
    BitNot,

    Equal,
    NotEqual,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,

    LogicalAnd,
    LogicalOr,
    LogicalNot,

    Branch,
    ConditionalBranch,
    Return,
    Unreachable,

    Call,
    CallNative,

    Load,
    Store,
    Alloca,
    LoadField,
    StoreField,

    Cast,
    Bitcast,

    ConstructVector,
    ExtractElement,
    Swizzle,

    ConstructArray,
    ArrayGet,
    ArraySet,
    ArrayLength,

    NewObject,
    GetField,
    SetField,

    SpawnEntity,
    DestroyEntity,
    AddComponent,
    GetComponent,
    SetComponent,
    RemoveComponent,
    HasComponent,
    DefineComponent,
    DefineSystem,
    SystemSchedule,
    QueryAll,
    QueryAny,
    QueryWith,
    QueryWithout,
    WorldUpdate,

    MakeClosure,
    GetUpvalue,
    SetUpvalue,

    IsNull,
    IsType,
    TypeOf,

    Phi,

    Yield,
    Resume,

    Discard
}

public sealed class IrInstruction
{
    #region Properties

    public IrOpcode Opcode { get; }

    public IrValue? Result { get; }

    public IReadOnlyList<IrValue> Operands { get; }

    public IReadOnlyList<object> Arguments { get; }

    public SourceSpan? Span { get; }

    public BasicBlock? Parent { get; internal set; }

    #endregion

    #region Constructors

    public IrInstruction(IrOpcode opcode, IrValue? result = null,
        IReadOnlyList<IrValue>? operands = null,
        IReadOnlyList<object>? arguments = null,
        SourceSpan? span = null)
    {
        Opcode = opcode;
        Result = result;
        Operands = operands ?? [];
        Arguments = arguments ?? [];
        Span = span;
    }

    #endregion

    #region Factory Methods - Arithmetic

    public static IrInstruction Add(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Add, result, [lhs, rhs], span: span);

    public static IrInstruction Sub(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Sub, result, [lhs, rhs], span: span);

    public static IrInstruction Mul(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Mul, result, [lhs, rhs], span: span);

    public static IrInstruction Div(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Div, result, [lhs, rhs], span: span);

    public static IrInstruction Mod(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Mod, result, [lhs, rhs], span: span);

    public static IrInstruction Neg(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.Neg, result, [operand], span: span);

    #endregion

    #region Factory Methods - Bitwise

    public static IrInstruction Shl(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Shl, result, [lhs, rhs], span: span);

    public static IrInstruction Shr(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Shr, result, [lhs, rhs], span: span);

    public static IrInstruction BitAnd(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.BitAnd, result, [lhs, rhs], span: span);

    public static IrInstruction BitOr(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.BitOr, result, [lhs, rhs], span: span);

    public static IrInstruction BitXor(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.BitXor, result, [lhs, rhs], span: span);

    public static IrInstruction BitNot(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.BitNot, result, [operand], span: span);

    #endregion

    #region Factory Methods - Comparison

    public static IrInstruction Equal(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Equal, result, [lhs, rhs], span: span);

    public static IrInstruction NotEqual(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.NotEqual, result, [lhs, rhs], span: span);

    public static IrInstruction Less(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Less, result, [lhs, rhs], span: span);

    public static IrInstruction Greater(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.Greater, result, [lhs, rhs], span: span);

    public static IrInstruction LessEqual(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.LessEqual, result, [lhs, rhs], span: span);

    public static IrInstruction GreaterEqual(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.GreaterEqual, result, [lhs, rhs], span: span);

    #endregion

    #region Factory Methods - Logical

    public static IrInstruction LogicalAnd(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.LogicalAnd, result, [lhs, rhs], span: span);

    public static IrInstruction LogicalOr(IrValue result, IrValue lhs, IrValue rhs, SourceSpan? span = null)
        => new(IrOpcode.LogicalOr, result, [lhs, rhs], span: span);

    public static IrInstruction LogicalNot(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.LogicalNot, result, [operand], span: span);

    #endregion

    #region Factory Methods - Control Flow

    public static IrInstruction Branch(BasicBlock target, SourceSpan? span = null)
        => new(IrOpcode.Branch, result: null, operands: null, arguments: [target], span: span);

    public static IrInstruction ConditionalBranch(IrValue condition, BasicBlock trueTarget, BasicBlock falseTarget, SourceSpan? span = null)
        => new(IrOpcode.ConditionalBranch, result: null, operands: [condition], arguments: [trueTarget, falseTarget], span: span);

    public static IrInstruction Return(IrValue? value = null, SourceSpan? span = null)
        => new(IrOpcode.Return, result: null, operands: value is not null ? [value] : null, span: span);

    public static IrInstruction Unreachable(SourceSpan? span = null)
        => new(IrOpcode.Unreachable, span: span);

    #endregion

    #region Factory Methods - Call

    public static IrInstruction Call(IrValue result, string functionName, IReadOnlyList<IrValue> args, SourceSpan? span = null)
        => new(IrOpcode.Call, result, args, [functionName], span);

    public static IrInstruction CallNative(IrValue result, string nativeName, IReadOnlyList<IrValue> args, SourceSpan? span = null)
        => new(IrOpcode.CallNative, result, args, [nativeName], span);

    #endregion

    #region Factory Methods - Memory

    public static IrInstruction Load(IrValue result, IrValue pointer, SourceSpan? span = null)
        => new(IrOpcode.Load, result, [pointer], span: span);

    public static IrInstruction Store(IrValue value, IrValue pointer, SourceSpan? span = null)
        => new(IrOpcode.Store, result: null, operands: [value, pointer], span: span);

    public static IrInstruction Alloca(IrValue result, IrType allocatedType, SourceSpan? span = null)
        => new(IrOpcode.Alloca, result, arguments: [allocatedType], span: span);

    public static IrInstruction LoadField(IrValue result, IrValue obj, string fieldName, SourceSpan? span = null)
        => new(IrOpcode.LoadField, result, [obj], [fieldName], span);

    public static IrInstruction StoreField(IrValue value, IrValue obj, string fieldName, SourceSpan? span = null)
        => new(IrOpcode.StoreField, result: null, operands: [value, obj], arguments: [fieldName], span: span);

    #endregion

    #region Factory Methods - Conversion

    public static IrInstruction Cast(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.Cast, result, [operand], span: span);

    public static IrInstruction Bitcast(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.Bitcast, result, [operand], span: span);

    #endregion

    #region Factory Methods - Vector

    public static IrInstruction ConstructVector(IrValue result, IReadOnlyList<IrValue> elements, SourceSpan? span = null)
        => new(IrOpcode.ConstructVector, result, elements, span: span);

    public static IrInstruction ExtractElement(IrValue result, IrValue vector, IrValue index, SourceSpan? span = null)
        => new(IrOpcode.ExtractElement, result, [vector, index], span: span);

    public static IrInstruction Swizzle(IrValue result, IrValue vector, string mask, SourceSpan? span = null)
        => new(IrOpcode.Swizzle, result, [vector], [mask], span);

    #endregion

    #region Factory Methods - Array

    public static IrInstruction ConstructArray(IrValue result, IReadOnlyList<IrValue> elements, SourceSpan? span = null)
        => new(IrOpcode.ConstructArray, result, elements, span: span);

    public static IrInstruction ArrayGet(IrValue result, IrValue array, IrValue index, SourceSpan? span = null)
        => new(IrOpcode.ArrayGet, result, [array, index], span: span);

    public static IrInstruction ArraySet(IrValue array, IrValue index, IrValue value, SourceSpan? span = null)
        => new(IrOpcode.ArraySet, result: null, operands: [array, index, value], span: span);

    public static IrInstruction ArrayLength(IrValue result, IrValue array, SourceSpan? span = null)
        => new(IrOpcode.ArrayLength, result, [array], span: span);

    #endregion

    #region Factory Methods - Object

    public static IrInstruction NewObject(IrValue result, string typeName, IReadOnlyList<IrValue> args, SourceSpan? span = null)
        => new(IrOpcode.NewObject, result, args, [typeName], span);

    public static IrInstruction GetField(IrValue result, IrValue obj, string fieldName, SourceSpan? span = null)
        => new(IrOpcode.GetField, result, [obj], [fieldName], span);

    public static IrInstruction SetField(IrValue obj, string fieldName, IrValue value, SourceSpan? span = null)
        => new(IrOpcode.SetField, result: null, operands: [obj, value], arguments: [fieldName], span: span);

    #endregion

    #region Factory Methods - ECS

    public static IrInstruction SpawnEntity(IrValue result, SourceSpan? span = null)
        => new(IrOpcode.SpawnEntity, result, span: span);

    public static IrInstruction DestroyEntity(IrValue entity, SourceSpan? span = null)
        => new(IrOpcode.DestroyEntity, result: null, operands: [entity], span: span);

    public static IrInstruction AddComponent(IrValue entity, IrValue component, SourceSpan? span = null)
        => new(IrOpcode.AddComponent, result: null, operands: [entity, component], span: span);

    public static IrInstruction GetComponent(IrValue result, IrValue entity, string componentType, SourceSpan? span = null)
        => new(IrOpcode.GetComponent, result, [entity], [componentType], span);

    public static IrInstruction SetComponent(IrValue entity, string componentType, IrValue value, SourceSpan? span = null)
        => new(IrOpcode.SetComponent, result: null, operands: [entity, value], arguments: [componentType], span: span);

    public static IrInstruction RemoveComponent(IrValue entity, string componentType, SourceSpan? span = null)
        => new(IrOpcode.RemoveComponent, result: null, operands: [entity], arguments: [componentType], span: span);

    public static IrInstruction HasComponent(IrValue result, IrValue entity, string componentType, SourceSpan? span = null)
        => new(IrOpcode.HasComponent, result, [entity], [componentType], span);

    public static IrInstruction DefineComponent(IrValue result, string componentType, IReadOnlyList<(string Name, IrType Type)> fields, SourceSpan? span = null)
        => new(IrOpcode.DefineComponent, result, arguments: [componentType, .. fields.Select(f => (object)f)], span: span);

    public static IrInstruction DefineSystem(IrValue result, string systemName, string phase, IrValue function, SourceSpan? span = null)
        => new(IrOpcode.DefineSystem, result, [function], [systemName, phase], span);

    public static IrInstruction SystemSchedule(IrValue system, IReadOnlyList<IrValue> dependencies, SourceSpan? span = null)
        => new(IrOpcode.SystemSchedule, result: null, operands: [system, .. dependencies], span: span);

    public static IrInstruction QueryAll(IrValue result, string componentType, SourceSpan? span = null)
        => new(IrOpcode.QueryAll, result, arguments: [componentType], span: span);

    public static IrInstruction QueryAny(IrValue result, string componentType, SourceSpan? span = null)
        => new(IrOpcode.QueryAny, result, arguments: [componentType], span: span);

    public static IrInstruction QueryWith(IrValue result, IrValue query, string componentType, SourceSpan? span = null)
        => new(IrOpcode.QueryWith, result, [query], [componentType], span);

    public static IrInstruction QueryWithout(IrValue result, IrValue query, string componentType, SourceSpan? span = null)
        => new(IrOpcode.QueryWithout, result, [query], [componentType], span);

    public static IrInstruction WorldUpdate(IrValue deltaTime, SourceSpan? span = null)
        => new(IrOpcode.WorldUpdate, result: null, operands: [deltaTime], span: span);

    #endregion

    #region Factory Methods - Closure

    public static IrInstruction MakeClosure(IrValue result, string functionName, IReadOnlyList<IrValue> captured, SourceSpan? span = null)
        => new(IrOpcode.MakeClosure, result, captured, [functionName], span);

    public static IrInstruction GetUpvalue(IrValue result, IrValue closure, int index, SourceSpan? span = null)
        => new(IrOpcode.GetUpvalue, result, [closure], [index], span);

    public static IrInstruction SetUpvalue(IrValue closure, int index, IrValue value, SourceSpan? span = null)
        => new(IrOpcode.SetUpvalue, result: null, operands: [closure, value], arguments: [index], span: span);

    #endregion

    #region Factory Methods - Type Check

    public static IrInstruction IsNull(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.IsNull, result, [operand], span: span);

    public static IrInstruction IsType(IrValue result, IrValue operand, IrType type, SourceSpan? span = null)
        => new(IrOpcode.IsType, result, [operand], [type], span);

    public static IrInstruction TypeOf(IrValue result, IrValue operand, SourceSpan? span = null)
        => new(IrOpcode.TypeOf, result, [operand], span: span);

    #endregion

    #region Factory Methods - SSA

    public static IrInstruction Phi(IrValue result, IReadOnlyList<(IrValue Value, BasicBlock Block)> incoming, SourceSpan? span = null)
    {
        var operands = new List<IrValue>();
        var arguments = new List<object>();
        foreach (var (value, block) in incoming)
        {
            operands.Add(value);
            arguments.Add(block);
        }
        return new IrInstruction(IrOpcode.Phi, result, operands, arguments, span);
    }

    #endregion

    #region Factory Methods - Shader

    public static IrInstruction Discard(SourceSpan? span = null)
        => new(IrOpcode.Discard, span: span);

    #endregion

    #region Public Methods

    public bool IsTerminator() => Opcode is
        IrOpcode.Branch or IrOpcode.ConditionalBranch or IrOpcode.Return or IrOpcode.Unreachable or IrOpcode.Discard;

    public bool IsBranch() => Opcode is IrOpcode.Branch or IrOpcode.ConditionalBranch;

    public IEnumerable<BasicBlock> GetSuccessorBlocks()
    {
        foreach (var arg in Arguments)
        {
            if (arg is BasicBlock block)
            {
                yield return block;
            }
        }
    }

    public void ReplaceOperand(IrValue old, IrValue @new)
    {
        for (int i = 0; i < Operands.Count; i++)
        {
            if (ReferenceEquals(Operands[i], old))
            {
                var newOperands = new List<IrValue>(Operands);
                newOperands[i] = @new;
            }
        }
    }

    public override string ToString()
    {
        var result = Result is not null ? $"{Result} = " : "";
        var operands = string.Join(", ", Operands);
        var extraArgs = Arguments.Count > 0 ? $" [{string.Join(", ", Arguments)}]" : "";
        return $"{result}{Opcode} {operands}{extraArgs}";
    }

    #endregion
}
