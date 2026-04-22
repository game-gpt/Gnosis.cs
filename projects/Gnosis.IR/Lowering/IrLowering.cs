using Gnosis.IR.Graph;

namespace Gnosis.IR.Lowering;

public sealed class IrLowering
{
    #region Properties

    public LoweringContext Context { get; }

    #endregion

    #region Constructors

    public IrLowering(LoweringContext context)
    {
        Context = context;
    }

    #endregion

    #region Public Methods

    public IrFunction LowerFunction(string name, IrType returnType,
        IReadOnlyList<(string Name, IrType Type)> parameters,
        Action bodyEmitter,
        IReadOnlyList<string>? attributes = null)
    {
        var function = Context.Module.CreateFunction(name, returnType, parameters, attributes);
        Context.SetFunction(function);

        var entryBlock = function.CreateBlock("entry");
        Context.SetBlock(entryBlock);

        for (int i = 0; i < parameters.Count; i++)
        {
            var (paramName, paramType) = parameters[i];
            var paramValue = function.CreateValue(paramType, paramName);
            Context.BindValue(paramName, paramValue);
        }

        bodyEmitter();

        if (!entryBlock.IsTerminated)
        {
            Context.Emit(IrInstruction.Return());
        }

        return function;
    }

    public IrValue EmitConst(long value)
    {
        var result = Context.CreateValue(IrType.I32);
        Context.Emit(IrInstruction.Cast(result, result));
        return result;
    }

    public IrValue EmitConst(double value)
    {
        var result = Context.CreateValue(IrType.F32);
        Context.Emit(IrInstruction.Cast(result, result));
        return result;
    }

    public IrValue EmitConst(bool value)
    {
        var result = Context.CreateValue(IrType.Bool);
        Context.Emit(IrInstruction.Cast(result, result));
        return result;
    }

    public IrValue EmitBinary(IrOpcode opcode, IrValue left, IrValue right, IrType? resultType = null)
    {
        var result = Context.CreateValue(resultType ?? left.Type);
        Context.Emit(new IrInstruction(opcode, result, [left, right]));
        return result;
    }

    public IrValue EmitUnary(IrOpcode opcode, IrValue operand, IrType? resultType = null)
    {
        var result = Context.CreateValue(resultType ?? operand.Type);
        Context.Emit(new IrInstruction(opcode, result, [operand]));
        return result;
    }

    public IrValue EmitCall(string functionName, IReadOnlyList<IrValue> args, IrType returnType)
    {
        var result = Context.CreateValue(returnType);
        Context.Emit(IrInstruction.Call(result, functionName, args));
        return result;
    }

    public IrValue EmitNativeCall(string nativeName, IReadOnlyList<IrValue> args, IrType returnType)
    {
        var result = Context.CreateValue(returnType);
        Context.Emit(IrInstruction.CallNative(result, nativeName, args));
        return result;
    }

    public IrValue EmitAlloca(IrType allocatedType, string? name = null)
    {
        var result = Context.CreateValue(IrType.ReferenceTo(allocatedType), name);
        Context.Emit(IrInstruction.Alloca(result, allocatedType));
        return result;
    }

    public void EmitStore(IrValue value, IrValue pointer)
    {
        Context.Emit(IrInstruction.Store(value, pointer));
    }

    public IrValue EmitLoad(IrValue pointer, IrType loadedType)
    {
        var result = Context.CreateValue(loadedType);
        Context.Emit(IrInstruction.Load(result, pointer));
        return result;
    }

    public void EmitReturn(IrValue? value = null)
    {
        Context.Emit(IrInstruction.Return(value));
    }

    public void EmitBranch(BasicBlock target)
    {
        Context.Emit(IrInstruction.Branch(target));
    }

    public void EmitConditionalBranch(IrValue condition, BasicBlock trueTarget, BasicBlock falseTarget)
    {
        Context.Emit(IrInstruction.ConditionalBranch(condition, trueTarget, falseTarget));
    }

    public void EmitIf(IrValue condition, Action thenEmitter, Action? elseEmitter = null)
    {
        if (elseEmitter is not null)
        {
            var thenBlock = Context.CreateBlock("if.then");
            var elseBlock = Context.CreateBlock("if.else");
            var mergeBlock = Context.CreateBlock("if.merge");

            EmitConditionalBranch(condition, thenBlock, elseBlock);

            Context.SetBlock(thenBlock);
            thenEmitter();
            if (!Context.CurrentBlock.IsTerminated)
            {
                EmitBranch(mergeBlock);
            }

            Context.SetBlock(elseBlock);
            elseEmitter();
            if (!Context.CurrentBlock.IsTerminated)
            {
                EmitBranch(mergeBlock);
            }

            Context.SetBlock(mergeBlock);
        }
        else
        {
            var thenBlock = Context.CreateBlock("if.then");
            var mergeBlock = Context.CreateBlock("if.merge");

            EmitConditionalBranch(condition, thenBlock, mergeBlock);

            Context.SetBlock(thenBlock);
            thenEmitter();
            if (!Context.CurrentBlock.IsTerminated)
            {
                EmitBranch(mergeBlock);
            }

            Context.SetBlock(mergeBlock);
        }
    }

    public void EmitWhile(IrValue condition, Action bodyEmitter, Func<IrValue> conditionEmitter)
    {
        var condBlock = Context.CreateBlock("while.cond");
        var bodyBlock = Context.CreateBlock("while.body");
        var endBlock = Context.CreateBlock("while.end");

        EmitBranch(condBlock);

        Context.SetBlock(condBlock);
        var cond = conditionEmitter();
        EmitConditionalBranch(cond, bodyBlock, endBlock);

        Context.SetBlock(bodyBlock);
        bodyEmitter();
        if (!Context.CurrentBlock.IsTerminated)
        {
            EmitBranch(condBlock);
        }

        Context.SetBlock(endBlock);
    }

    public IrValue EmitPhi(IrType type, IReadOnlyList<(IrValue Value, BasicBlock Block)> incoming)
    {
        var result = Context.CreateValue(type);
        Context.Emit(IrInstruction.Phi(result, incoming));
        return result;
    }

    #endregion
}
