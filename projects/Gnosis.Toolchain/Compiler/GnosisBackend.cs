using System.Text;
using Nyar.Core.Assembler;
using Nyar.Core.Types;
using Nyar.Dialect.Game.Rules;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis VM 后端，实现 ICodeGenBackend 接口
///     将 Nyar 模块编译为 Gnosis VM 字节码
///     核心职责：将 GameBuiltin ID (0x8001-0x800B) 映射为 Gnosis OpCode (0x80-0x8E)
/// </summary>
public sealed class GnosisBackend : ICodeGenBackend
{
    /// <summary>
    ///     后端名称
    /// </summary>
    public string Name => "GnosisVM";

    /// <summary>
    ///     后端支持的目标架构列表
    /// </summary>
    public IReadOnlyList<Arch> SupportedArchs => new[] { Arch.GnosisVm };

    /// <summary>
    ///     GameBuiltin ID → Gnosis VM 字节码操作码的映射表
    /// </summary>
    private static readonly Dictionary<long, byte> BuiltinToByteMap = new()
    {
        [(long)GameBuiltin.EcsSpawn] = 0x80,
        [(long)GameBuiltin.EcsDestroy] = 0x81,
        [(long)GameBuiltin.EcsAddComponent] = 0x82,
        [(long)GameBuiltin.EcsGetComponent] = 0x83,
        [(long)GameBuiltin.EcsSetComponent] = 0x89,
        [(long)GameBuiltin.EcsRemoveComponent] = 0x84,
        [(long)GameBuiltin.EcsHasComponent] = 0x8A,
        [(long)GameBuiltin.EcsDefineComponent] = 0x87,
        [(long)GameBuiltin.EcsDefineSystem] = 0x88,
        [(long)GameBuiltin.EcsQuery] = 0x85,
        [(long)GameBuiltin.EcsWorldUpdate] = 0x8E
    };

    /// <summary>
    ///     编译 Nyar 模块为 Gnosis VM 字节码
    /// </summary>
    /// <param name="module">通用编译单元</param>
    /// <param name="options">编译选项</param>
    /// <returns>编译生成的文件集合</returns>
    public GeneratedFiles Compile(CompilationUnit module, CompilationOptions options)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WriteHeader(writer);
        WriteConstantPool(writer, module.Constants);
        WriteFunctions(writer, module);

        var bytecode = stream.ToArray();

        var files = new GeneratedFiles();
        files.AddFile(new GeneratedFile(
            $"{module.Name}.gnosis",
            bytecode,
            "binary"));

        if (options.GenerateTextOutput)
        {
            var disassembly = Disassemble(module);
            files.AddFile(new GeneratedFile(
                $"{module.Name}.gnosis.asm",
                Encoding.UTF8.GetBytes(disassembly),
                "text"));
        }

        return files;
    }

    /// <summary>
    ///     验证模块是否可以被 Gnosis VM 后端编译
    /// </summary>
    /// <param name="module">通用编译单元</param>
    /// <param name="diagnostics">验证产生的诊断信息列表</param>
    /// <returns>验证是否通过</returns>
    public bool Validate(CompilationUnit module, out List<Diagnostic> diagnostics)
    {
        diagnostics = new List<Diagnostic>();
        return true;
    }

    /// <summary>
    ///     编译 Nyar 模块为 NyarVM 兼容字节码
    ///     将 GameBuiltin ID 映射为 BuiltinCall (0xE0) 指令，而非 Gnosis 专用操作码
    ///     用于 GnosisGameVM 通过 NyarVM 执行时的运行时分派
    /// </summary>
    /// <param name="module">通用编译单元</param>
    /// <returns>NyarVM 兼容的字节码</returns>
    public byte[] CompileForNyarVM(CompilationUnit module)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WriteHeader(writer);
        WriteConstantPool(writer, module.Constants);
        WriteFunctionsWithBuiltinCall(writer, module);

        return stream.ToArray();
    }

    #region 字节码写入

    private static void WriteHeader(BinaryWriter writer)
    {
        writer.Write(new byte[] { 0x47, 0x4E, 0x4F, 0x53 });
        writer.Write(1);
    }

    private static void WriteConstantPool(BinaryWriter writer, ConstantPool constants)
    {
        writer.Write(constants.Count);
    }

    private static void WriteFunctions(BinaryWriter writer, CompilationUnit module)
    {
        writer.Write(module.Functions.Count);

        foreach (var function in module.Functions)
        {
            writer.Write(function.Name);
            writer.Write(function.Parameters.Count);

            using var codeStream = new MemoryStream();
            using var codeWriter = new BinaryWriter(codeStream);

            foreach (var instruction in function.Instructions)
            {
                EmitInstruction(codeWriter, instruction, module.Constants);
            }

            var codeBytes = codeStream.ToArray();
            writer.Write(codeBytes.Length);
            writer.Write(codeBytes);
        }
    }

    private static void EmitInstruction(BinaryWriter writer, Instruction instruction, ConstantPool constants)
    {
        if (TryEmitBuiltinCall(writer, instruction, constants))
        {
            return;
        }

        switch (instruction.Opcode)
        {
            case 0x00:
                writer.Write((byte)0x00);
                break;
            case 0x01:
                writer.Write((byte)0x01);
                break;
            case 0x04:
                writer.Write((byte)0x50);
                if (instruction.Operands.Count > 0)
                {
                    writer.Write((int)instruction.Operands[0].IntValue);
                }
                break;
            case 0x05:
                writer.Write((byte)0x52);
                break;
            default:
                writer.Write((byte)0x00);
                break;
        }
    }

    /// <summary>
    ///     尝试将内置函数调用（GameBuiltin ID）映射为 Gnosis VM ECS 操作码
    ///     这是 GameBuiltin (0x8001-0x800B) → Gnosis OpCode (0x80-0x8E) 的核心桥接
    /// </summary>
    private static bool TryEmitBuiltinCall(BinaryWriter writer, Instruction instruction, ConstantPool constants)
    {
        if (instruction.Opcode != 0x50 && instruction.Opcode != 0x51)
        {
            return false;
        }

        if (instruction.Operands.Count == 0)
        {
            return false;
        }

        var funcIdOperand = instruction.Operands[0];
        if (funcIdOperand.Kind != OperandKind.Constant)
        {
            return false;
        }

        var constantIdx = (int)funcIdOperand.IntValue;
        long builtinId;
        try
        {
            builtinId = constants.GetInt64(constantIdx);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (!BuiltinToByteMap.TryGetValue(builtinId, out var gnosisOpCode))
        {
            return false;
        }

        writer.Write(gnosisOpCode);

        for (var i = 1; i < instruction.Operands.Count; i++)
        {
            var operand = instruction.Operands[i];
            if (operand.Kind == OperandKind.Constant)
            {
                writer.Write((int)operand.IntValue);
            }
            else if (operand.Kind == OperandKind.Immediate)
            {
                writer.Write((int)operand.IntValue);
            }
        }

        return true;
    }

    private static string Disassemble(CompilationUnit module)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; Gnosis VM 字节码 - {module.Name}");
        sb.AppendLine();

        foreach (var function in module.Functions)
        {
            sb.AppendLine($"fn {function.Name}({function.Parameters.Count} params):");
            sb.AppendLine("  ; 指令待反汇编");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     写入函数段（NyarVM 兼容模式）
    ///     GameBuiltin 调用映射为 BuiltinCall (0xE0) 指令
    ///     指令格式：[0xE0][builtinId:i64][argCount:i32]
    /// </summary>
    private static void WriteFunctionsWithBuiltinCall(BinaryWriter writer, CompilationUnit module)
    {
        writer.Write(module.Functions.Count);

        foreach (var function in module.Functions)
        {
            writer.Write(function.Name);
            writer.Write(function.Parameters.Count);

            using var codeStream = new MemoryStream();
            using var codeWriter = new BinaryWriter(codeStream);

            foreach (var instruction in function.Instructions)
            {
                EmitInstructionWithBuiltinCall(codeWriter, instruction, module.Constants);
            }

            var codeBytes = codeStream.ToArray();
            writer.Write(codeBytes.Length);
            writer.Write(codeBytes);
        }
    }

    /// <summary>
    ///     发射指令（NyarVM 兼容模式）
    ///     GameBuiltin 调用映射为 BuiltinCall (0xE0) 指令
    /// </summary>
    private static void EmitInstructionWithBuiltinCall(BinaryWriter writer, Instruction instruction, ConstantPool constants)
    {
        if (TryEmitBuiltinCallAsNyarVM(writer, instruction, constants))
        {
            return;
        }

        EmitInstruction(writer, instruction, constants);
    }

    /// <summary>
    ///     尝试将内置函数调用映射为 NyarVM BuiltinCall (0xE0) 指令
    ///     指令格式：[0xE0][builtinId:i64][argCount:i32]
    /// </summary>
    private static bool TryEmitBuiltinCallAsNyarVM(BinaryWriter writer, Instruction instruction, ConstantPool constants)
    {
        if (instruction.Opcode != 0x50 && instruction.Opcode != 0x51)
        {
            return false;
        }

        if (instruction.Operands.Count == 0)
        {
            return false;
        }

        var funcIdOperand = instruction.Operands[0];
        if (funcIdOperand.Kind != OperandKind.Constant)
        {
            return false;
        }

        var constantIdx = (int)funcIdOperand.IntValue;
        long builtinId;
        try
        {
            builtinId = constants.GetInt64(constantIdx);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (!BuiltinToByteMap.ContainsKey(builtinId))
        {
            return false;
        }

        writer.Write((byte)0xE0);
        writer.Write(builtinId);
        writer.Write(instruction.Operands.Count - 1);

        return true;
    }

    #endregion
}
