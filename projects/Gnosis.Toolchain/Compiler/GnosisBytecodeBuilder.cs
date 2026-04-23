using Gnosis.ECS;
using Gnosis.ECS.Entity;
using Gnosis.ECS.World;
using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;
using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 字节码构建器，将 Nyar IR 编译为 Gnosis 引擎字节码
/// </summary>
public sealed class GnosisBytecodeBuilder : IGnosisCompilerBackend
{
    /// <summary>
    ///     后端名称
    /// </summary>
    public string Name => "Gnosis";

    /// <summary>
    ///     编译 IR 模块为 Gnosis 字节码单元
    /// </summary>
    public BytecodeUnit Compile(IrModule module, GnosisCompileOptions options)
    {
        var functions = new List<BytecodeFunction>();
        var constants = new List<object>();
        var imports = new List<string>(module.Imports);
        var exports = new List<string>(module.Exports);
        var sourceMap = new List<(int Offset, SourceSpan? Span)>();

        foreach (var function in module.Functions)
        {
            var instructions = new List<BytecodeInstruction>();
            var offset = 0;

            foreach (var block in function.Blocks)
            {
                foreach (var instr in block.Instructions)
                {
                    var bytecodes = ConvertInstruction(instr, constants);

                    foreach (var bytecode in bytecodes)
                    {
                        instructions.Add(bytecode);
                        sourceMap.Add((offset, instr.Span));
                        offset += 1 + GetOperandSize(bytecode.OpCode);
                    }
                }
            }

            var parameterCount = function.Parameters?.Count ?? 0;
            var localCount = 0;

            functions.Add(new BytecodeFunction(
                function.Name,
                parameterCount,
                localCount,
                instructions));
        }

        return new BytecodeUnit(
            module.Name,
            constants,
            functions,
            imports,
            exports,
            sourceMap);
    }

    /// <summary>
    ///     验证 IR 模块是否可以被 Gnosis 后端编译
    /// </summary>
    public bool Validate(IrModule module, out List<string> diagnostics)
    {
        diagnostics = new List<string>();
        var isValid = true;

        foreach (var function in module.Functions)
        {
            if (string.IsNullOrEmpty(function.Name))
            {
                diagnostics.Add("函数名称不能为空");
                isValid = false;
            }

            foreach (var block in function.Blocks)
            {
                foreach (var instr in block.Instructions)
                {
                    if (!IsSupportedOpcode(instr.Opcode))
                    {
                        diagnostics.Add($"不支持的指令操作码: {instr.Opcode}");
                        isValid = false;
                    }
                }
            }
        }

        return isValid;
    }

    #region 指令转换

    private static List<BytecodeInstruction> ConvertInstruction(IrInstruction instr, List<object> constants)
    {
        var result = new List<BytecodeInstruction>();

        switch (instr.Opcode)
        {
            case IrOpcode.Nop:
                result.Add(new BytecodeInstruction(OpCode.Nop));
                break;

            case IrOpcode.Return:
                result.Add(new BytecodeInstruction(OpCode.Return));
                break;

            case IrOpcode.Branch:
                result.Add(new BytecodeInstruction(OpCode.Jump, GetBranchTarget(instr)));
                break;

            case IrOpcode.ConditionalBranch:
                result.Add(new BytecodeInstruction(OpCode.JumpIfTrue, GetBranchTarget(instr)));
                break;

            case IrOpcode.Call:
                result.Add(new BytecodeInstruction(OpCode.Call, GetCallTarget(instr)));
                break;

            case IrOpcode.Add:
                result.Add(new BytecodeInstruction(OpCode.AddInt));
                break;

            case IrOpcode.Sub:
                result.Add(new BytecodeInstruction(OpCode.SubInt));
                break;

            case IrOpcode.Mul:
                result.Add(new BytecodeInstruction(OpCode.MulInt));
                break;

            case IrOpcode.Div:
                result.Add(new BytecodeInstruction(OpCode.DivInt));
                break;

            case IrOpcode.Load:
                result.Add(new BytecodeInstruction(OpCode.LoadLocal, GetOperandIndex(instr)));
                break;

            case IrOpcode.Store:
                result.Add(new BytecodeInstruction(OpCode.StoreLocal, GetOperandIndex(instr)));
                break;

            case IrOpcode.SpawnEntity:
                result.Add(new BytecodeInstruction(OpCode.SpawnEntity));
                break;

            case IrOpcode.DestroyEntity:
                result.Add(new BytecodeInstruction(OpCode.DestroyEntity));
                break;

            case IrOpcode.AddComponent:
                result.Add(new BytecodeInstruction(OpCode.AddComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.GetComponent:
                result.Add(new BytecodeInstruction(OpCode.GetComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.RemoveComponent:
                result.Add(new BytecodeInstruction(OpCode.RemoveComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.QueryAll:
                result.Add(new BytecodeInstruction(OpCode.QueryAll, GetQueryTypeIndex(instr, constants)));
                break;

            case IrOpcode.QueryAny:
                result.Add(new BytecodeInstruction(OpCode.QueryAny, GetQueryTypeIndex(instr, constants)));
                break;

            default:
                result.Add(new BytecodeInstruction(OpCode.Nop));
                break;
        }

        return result;
    }

    private static int GetBranchTarget(IrInstruction instr)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is BasicBlock block)
        {
            return block.Id;
        }
        return 0;
    }

    private static int GetCallTarget(IrInstruction instr)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is string funcName)
        {
            return funcName.GetHashCode();
        }
        return 0;
    }

    private static int GetOperandIndex(IrInstruction instr)
    {
        if (instr.Operands.Count > 0)
        {
            return instr.Operands[0].Id;
        }
        return 0;
    }

    private static int GetComponentTypeIndex(IrInstruction instr, List<object> constants)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is string typeName)
        {
            var idx = constants.Count;
            constants.Add(typeName);
            return idx;
        }
        return 0;
    }

    private static int GetQueryTypeIndex(IrInstruction instr, List<object> constants)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is string typeName)
        {
            var idx = constants.Count;
            constants.Add(typeName);
            return idx;
        }
        return 0;
    }

    #endregion

    #region 辅助方法

    private static bool IsSupportedOpcode(IrOpcode opCode)
    {
        return opCode is
            IrOpcode.Nop or
            IrOpcode.Return or
            IrOpcode.Branch or
            IrOpcode.ConditionalBranch or
            IrOpcode.Call or
            IrOpcode.Add or
            IrOpcode.Sub or
            IrOpcode.Mul or
            IrOpcode.Div or
            IrOpcode.Load or
            IrOpcode.Store or
            IrOpcode.SpawnEntity or
            IrOpcode.DestroyEntity or
            IrOpcode.AddComponent or
            IrOpcode.GetComponent or
            IrOpcode.RemoveComponent or
            IrOpcode.QueryAll or
            IrOpcode.QueryAny;
    }

    private static int GetOperandSize(OpCode opCode)
    {
        return opCode switch
        {
            OpCode.PushInt8 => 1,
            OpCode.PushInt16 => 2,
            OpCode.PushInt64 => 8,
            OpCode.PushFloat64 => 8,
            OpCode.PushInt32 or OpCode.PushFloat32 or OpCode.Jump or OpCode.JumpIfTrue
                or OpCode.JumpIfFalse or OpCode.Call or OpCode.CallNative
                or OpCode.LoadLocal or OpCode.StoreLocal or OpCode.LoadGlobal
                or OpCode.StoreGlobal or OpCode.LoadField or OpCode.StoreField
                or OpCode.NewObject or OpCode.GetField or OpCode.SetField
                or OpCode.AddComponent or OpCode.GetComponent or OpCode.RemoveComponent
                or OpCode.PushString or OpCode.NewArray or OpCode.MakeClosure
                or OpCode.IsType or OpCode.TypeOf or OpCode.QueryAll or OpCode.QueryAny => 4,
            _ => 0
        };
    }

    #endregion
}
