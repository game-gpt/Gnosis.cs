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

            case IrOpcode.SetComponent:
                result.Add(new BytecodeInstruction(OpCode.SetComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.RemoveComponent:
                result.Add(new BytecodeInstruction(OpCode.RemoveComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.HasComponent:
                result.Add(new BytecodeInstruction(OpCode.HasComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.DefineComponent:
                result.Add(new BytecodeInstruction(OpCode.DefineComponent, GetComponentTypeIndex(instr, constants)));
                break;

            case IrOpcode.DefineSystem:
                result.Add(new BytecodeInstruction(OpCode.DefineSystem, GetSystemNameIndex(instr, constants)));
                break;

            case IrOpcode.SystemSchedule:
                result.Add(new BytecodeInstruction(OpCode.SystemSchedule));
                break;

            case IrOpcode.QueryAll:
                result.Add(new BytecodeInstruction(OpCode.QueryAll, GetQueryTypeIndex(instr, constants)));
                break;

            case IrOpcode.QueryAny:
                result.Add(new BytecodeInstruction(OpCode.QueryAny, GetQueryTypeIndex(instr, constants)));
                break;

            case IrOpcode.QueryWith:
                result.Add(new BytecodeInstruction(OpCode.QueryWith, GetQueryTypeIndex(instr, constants)));
                break;

            case IrOpcode.QueryWithout:
                result.Add(new BytecodeInstruction(OpCode.QueryWithout, GetQueryTypeIndex(instr, constants)));
                break;

            case IrOpcode.WorldUpdate:
                result.Add(new BytecodeInstruction(OpCode.WorldUpdate));
                break;

            case IrOpcode.CallNative:
                ConvertCallNative(instr, constants, result);
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

    private static int GetSystemNameIndex(IrInstruction instr, List<object> constants)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is string systemName)
        {
            var idx = constants.Count;
            constants.Add(systemName);
            return idx;
        }
        return 0;
    }

    private static void ConvertCallNative(IrInstruction instr, List<object> constants, List<BytecodeInstruction> result)
    {
        if (instr.Arguments.Count == 0 || instr.Arguments[0] is not string nativeName)
        {
            result.Add(new BytecodeInstruction(OpCode.Nop));
            return;
        }

        switch (nativeName)
        {
            case "push_i32":
            {
                var value = instr.Arguments.Count > 1 && instr.Arguments[1] is int i32Val ? i32Val : 0;
                var idx = constants.Count;
                constants.Add(value);
                result.Add(new BytecodeInstruction(OpCode.PushInt32, idx));
                break;
            }

            case "push_f64":
            {
                var value = instr.Arguments.Count > 1 && instr.Arguments[1] is double f64Val ? f64Val : 0.0;
                var idx = constants.Count;
                constants.Add(value);
                result.Add(new BytecodeInstruction(OpCode.PushFloat64, idx));
                break;
            }

            case "push_f32":
            {
                var value = instr.Arguments.Count > 1 && instr.Arguments[1] is float f32Val ? f32Val : 0.0f;
                var idx = constants.Count;
                constants.Add(value);
                result.Add(new BytecodeInstruction(OpCode.PushFloat32, idx));
                break;
            }

            case "push_bool":
            {
                var value = instr.Arguments.Count > 1 && instr.Arguments[1] is int boolVal ? boolVal : 0;
                result.Add(new BytecodeInstruction(value != 0 ? OpCode.PushTrue : OpCode.PushFalse));
                break;
            }

            case "push_null":
                result.Add(new BytecodeInstruction(OpCode.PushNull));
                break;

            case "push_string":
            {
                var strValue = instr.Arguments.Count > 1 && instr.Arguments[1] is string str ? str : "";
                var idx = constants.Count;
                constants.Add(strValue);
                result.Add(new BytecodeInstruction(OpCode.PushString, idx));
                break;
            }

            default:
                result.Add(new BytecodeInstruction(OpCode.CallNative, GetNativeNameIndex(instr, constants)));
                break;
        }
    }

    private static int GetNativeNameIndex(IrInstruction instr, List<object> constants)
    {
        if (instr.Arguments.Count > 0 && instr.Arguments[0] is string nativeName)
        {
            var idx = constants.Count;
            constants.Add(nativeName);
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
            IrOpcode.CallNative or
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
            IrOpcode.SetComponent or
            IrOpcode.RemoveComponent or
            IrOpcode.HasComponent or
            IrOpcode.DefineComponent or
            IrOpcode.DefineSystem or
            IrOpcode.SystemSchedule or
            IrOpcode.QueryAll or
            IrOpcode.QueryAny or
            IrOpcode.QueryWith or
            IrOpcode.QueryWithout or
            IrOpcode.WorldUpdate;
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
                or OpCode.SetComponent or OpCode.HasComponent or OpCode.DefineComponent
                or OpCode.DefineSystem or OpCode.QueryAll or OpCode.QueryAny
                or OpCode.QueryWith or OpCode.QueryWithout or OpCode.SystemSchedule
                or OpCode.PushString or OpCode.NewArray or OpCode.MakeClosure
                or OpCode.IsType or OpCode.TypeOf => 4,
            _ => 0
        };
    }

    #endregion
}
