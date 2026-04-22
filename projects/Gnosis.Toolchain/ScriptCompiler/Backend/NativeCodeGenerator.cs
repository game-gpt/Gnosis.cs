using System.Text;
using Gnosis.IR.Instruction;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public sealed class NativeCodeGenerator
{
    #region Fields

    private readonly StringBuilder _sb = new();
    private int _stackOffset;

    #endregion

    #region Public Methods

    public string Generate(BytecodeUnit unit, TargetPlatform target)
    {
        _sb.Clear();
        _stackOffset = 0;

        EmitHeader(unit, target);

        foreach (var function in unit.Functions)
        {
            EmitFunction(function, target);
        }

        EmitFooter(unit, target);

        return _sb.ToString();
    }

    #endregion

    #region Private Methods - Header/Footer

    private void EmitHeader(BytecodeUnit unit, TargetPlatform target)
    {
        switch (target)
        {
            case TargetPlatform.X64:
                _sb.AppendLine("; Gnosis VM - x64 汇编输出");
                _sb.AppendLine($"; 模块: {unit.ModuleName}");
                _sb.AppendLine($"; 生成时间: {DateTime.UtcNow:O}");
                _sb.AppendLine();
                _sb.AppendLine("section .text");
                _sb.AppendLine("global gnosis_vm_entry");
                _sb.AppendLine();
                break;

            case TargetPlatform.ARM64:
                _sb.AppendLine("// Gnosis VM - ARM64 汇编输出");
                _sb.AppendLine($"// 模块: {unit.ModuleName}");
                _sb.AppendLine($"// 生成时间: {DateTime.UtcNow:O}");
                _sb.AppendLine();
                break;

            case TargetPlatform.WASM:
                _sb.AppendLine("(module");
                _sb.AppendLine("  ;; Gnosis VM - WebAssembly 输出");
                _sb.AppendLine($"  ;; 模块: {unit.ModuleName}");
                _sb.AppendLine($"  ;; 生成时间: {DateTime.UtcNow:O}");
                _sb.AppendLine();
                EmitWasmMemoryDecls(unit);
                break;
        }
    }

    private void EmitFooter(BytecodeUnit unit, TargetPlatform target)
    {
        if (target == TargetPlatform.WASM)
        {
            _sb.AppendLine(")");
        }
    }

    private void EmitWasmMemoryDecls(BytecodeUnit unit)
    {
        var constCount = Math.Max(unit.Constants.Count, 1);
        var memoryPages = (constCount * 8 + 65535) / 65536;

        _sb.AppendLine($"  (memory (export \"memory\") {memoryPages})");
        _sb.AppendLine();
    }

    #endregion

    #region Private Methods - Function Emission

    private void EmitFunction(BytecodeFunction function, TargetPlatform target)
    {
        switch (target)
        {
            case TargetPlatform.X64:
                EmitX64Function(function);
                break;
            case TargetPlatform.ARM64:
                EmitARM64Function(function);
                break;
            case TargetPlatform.WASM:
                EmitWasmFunction(function);
                break;
        }
    }

    private void EmitX64Function(BytecodeFunction function)
    {
        var funcName = MangleName(function.Name);

        _sb.AppendLine($"{funcName}:");
        _sb.AppendLine("    push rbp");
        _sb.AppendLine("    mov rbp, rsp");
        _sb.AppendLine($"    sub rsp, {function.LocalCount * 8}");

        _stackOffset = 0;

        foreach (var instr in function.Instructions)
        {
            EmitX64Instruction(instr);
        }

        _sb.AppendLine();
    }

    private void EmitARM64Function(BytecodeFunction function)
    {
        var funcName = MangleName(function.Name);

        _sb.AppendLine($"// 函数: {function.Name}");
        _sb.AppendLine($"  .global {funcName}");
        _sb.AppendLine($"  .align 2");
        _sb.AppendLine($"{funcName}:");
        _sb.AppendLine("  stp x29, x30, [sp, #-16]!");
        _sb.AppendLine("  mov x29, sp");
        _sb.AppendLine($"  sub sp, sp, #{function.LocalCount * 8}");

        _stackOffset = 0;

        foreach (var instr in function.Instructions)
        {
            EmitARM64Instruction(instr);
        }

        _sb.AppendLine();
    }

    private void EmitWasmFunction(BytecodeFunction function)
    {
        var funcName = SanitizeWasmName(function.Name);
        var paramSig = string.Join(" ", Enumerable.Repeat("i64", function.ParameterCount));
        var localSig = string.Join(" ", Enumerable.Repeat("i64", function.LocalCount));

        _sb.AppendLine($"  (func ${funcName} (export \"{funcName}\")");

        if (!string.IsNullOrEmpty(paramSig))
        {
            _sb.AppendLine($"    (param {paramSig})");
        }

        _sb.AppendLine("    (result i64)");
        _sb.AppendLine($"    (local {localSig})");

        foreach (var instr in function.Instructions)
        {
            EmitWasmInstruction(instr);
        }

        _sb.AppendLine("  )");
        _sb.AppendLine();
    }

    #endregion

    #region Private Methods - x64 Instruction Emission

    private void EmitX64Instruction(BytecodeInstruction instr)
    {
        switch (instr.OpCode)
        {
            case OpCode.PushInt64:
                _sb.AppendLine($"    mov rax, {instr.Operand}");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.PushInt32:
                _sb.AppendLine($"    mov rax, {instr.Operand}");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.LoadLocal:
                _sb.AppendLine($"    mov rax, [rbp-{(instr.Operand + 1) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.StoreLocal:
                _stackOffset--;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(instr.Operand + 1) * 8}], rax");
                break;

            case OpCode.AddInt:
                _stackOffset -= 2;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    add rax, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.SubInt:
                _stackOffset -= 2;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    sub rax, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.MulInt:
                _stackOffset -= 2;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    imul rax, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.DivInt:
                _stackOffset -= 2;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("    cqo");
                _sb.AppendLine($"    idiv qword [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.AddFloat:
                _stackOffset -= 2;
                _sb.AppendLine($"    movsd xmm0, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    addsd xmm0, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    movsd [rbp-{(_stackOffset + 1) * 8}], xmm0");
                _stackOffset++;
                break;

            case OpCode.SubFloat:
                _stackOffset -= 2;
                _sb.AppendLine($"    movsd xmm0, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    subsd xmm0, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    movsd [rbp-{(_stackOffset + 1) * 8}], xmm0");
                _stackOffset++;
                break;

            case OpCode.MulFloat:
                _stackOffset -= 2;
                _sb.AppendLine($"    movsd xmm0, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    mulsd xmm0, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    movsd [rbp-{(_stackOffset + 1) * 8}], xmm0");
                _stackOffset++;
                break;

            case OpCode.DivFloat:
                _stackOffset -= 2;
                _sb.AppendLine($"    movsd xmm0, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine($"    divsd xmm0, [rbp-{(_stackOffset + 2) * 8}]");
                _sb.AppendLine($"    movsd [rbp-{(_stackOffset + 1) * 8}], xmm0");
                _stackOffset++;
                break;

            case OpCode.Jump:
                _sb.AppendLine($"    jmp .L{instr.Operand}");
                break;

            case OpCode.JumpIfFalse:
                _stackOffset--;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("    test rax, rax");
                _sb.AppendLine($"    jz .L{instr.Operand}");
                break;

            case OpCode.JumpIfTrue:
                _stackOffset--;
                _sb.AppendLine($"    mov rax, [rbp-{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("    test rax, rax");
                _sb.AppendLine($"    jnz .L{instr.Operand}");
                break;

            case OpCode.Call:
                _sb.AppendLine($"    call {MangleName($"func_{instr.Operand}")}");
                _stackOffset++;
                break;

            case OpCode.CallNative:
                _sb.AppendLine($"    call {MangleName($"native_{instr.Operand}")}");
                _stackOffset++;
                break;

            case OpCode.Return:
                _sb.AppendLine("    mov rsp, rbp");
                _sb.AppendLine("    pop rbp");
                _sb.AppendLine("    ret");
                break;

            case OpCode.NegInt:
                _sb.AppendLine($"    neg qword [rbp-{_stackOffset * 8}]");
                break;

            case OpCode.NegFloat:
                _sb.AppendLine($"    movsd xmm0, [rbp-{_stackOffset * 8}]");
                _sb.AppendLine("    xorps xmm1, xmm1");
                _sb.AppendLine("    subsd xmm1, xmm0");
                _sb.AppendLine($"    movsd [rbp-{_stackOffset * 8}], xmm1");
                break;

            case OpCode.Dup:
                _sb.AppendLine($"    mov rax, [rbp-{_stackOffset * 8}]");
                _sb.AppendLine($"    mov [rbp-{(_stackOffset + 1) * 8}], rax");
                _stackOffset++;
                break;

            case OpCode.Pop:
                _stackOffset--;
                break;

            case OpCode.Halt:
                _sb.AppendLine("    mov rax, 0");
                _sb.AppendLine("    ret");
                break;

            default:
                _sb.AppendLine($"    ; 未实现的指令: {instr.OpCode} {instr.Operand}");
                break;
        }
    }

    #endregion

    #region Private Methods - ARM64 Instruction Emission

    private void EmitARM64Instruction(BytecodeInstruction instr)
    {
        switch (instr.OpCode)
        {
            case OpCode.PushInt64:
                _sb.AppendLine($"  mov x0, #{instr.Operand}");
                _sb.AppendLine($"  str x0, [sp, #{_stackOffset * 8}]");
                _stackOffset++;
                break;

            case OpCode.LoadLocal:
                _sb.AppendLine($"  ldr x0, [x29, #{-(instr.Operand + 1) * 8}]");
                _sb.AppendLine($"  str x0, [sp, #{_stackOffset * 8}]");
                _stackOffset++;
                break;

            case OpCode.StoreLocal:
                _stackOffset--;
                _sb.AppendLine($"  ldr x0, [sp, #{_stackOffset * 8}]");
                _sb.AppendLine($"  str x0, [x29, #{-(instr.Operand + 1) * 8}]");
                break;

            case OpCode.AddInt:
                _stackOffset -= 2;
                _sb.AppendLine($"  ldr x0, [sp, #{_stackOffset * 8}]");
                _sb.AppendLine($"  ldr x1, [sp, #{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("  add x0, x0, x1");
                _sb.AppendLine($"  str x0, [sp, #{_stackOffset * 8}]");
                _stackOffset++;
                break;

            case OpCode.SubInt:
                _stackOffset -= 2;
                _sb.AppendLine($"  ldr x0, [sp, #{_stackOffset * 8}]");
                _sb.AppendLine($"  ldr x1, [sp, #{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("  sub x0, x0, x1");
                _sb.AppendLine($"  str x0, [sp, #{_stackOffset * 8}]");
                _stackOffset++;
                break;

            case OpCode.MulInt:
                _stackOffset -= 2;
                _sb.AppendLine($"  ldr x0, [sp, #{_stackOffset * 8}]");
                _sb.AppendLine($"  ldr x1, [sp, #{(_stackOffset + 1) * 8}]");
                _sb.AppendLine("  mul x0, x0, x1");
                _sb.AppendLine($"  str x0, [sp, #{_stackOffset * 8}]");
                _stackOffset++;
                break;

            case OpCode.Jump:
                _sb.AppendLine($"  b .L{instr.Operand}");
                break;

            case OpCode.JumpIfFalse:
                _stackOffset--;
                _sb.AppendLine($"  ldr x0, [sp, #{_stackOffset * 8}]");
                _sb.AppendLine($"  cbz x0, .L{instr.Operand}");
                break;

            case OpCode.Return:
                _sb.AppendLine("  ldp x29, x30, [sp], #16");
                _sb.AppendLine("  ret");
                break;

            default:
                _sb.AppendLine($"  // 未实现的指令: {instr.OpCode} {instr.Operand}");
                break;
        }
    }

    #endregion

    #region Private Methods - WASM Instruction Emission

    private void EmitWasmInstruction(BytecodeInstruction instr)
    {
        switch (instr.OpCode)
        {
            case OpCode.PushInt64:
                _sb.AppendLine($"    i64.const {instr.Operand}");
                break;

            case OpCode.PushInt32:
                _sb.AppendLine($"    i32.const {instr.Operand}");
                break;

            case OpCode.PushFloat64:
                _sb.AppendLine($"    f64.const {instr.Operand}");
                break;

            case OpCode.PushFloat32:
                _sb.AppendLine($"    f32.const {instr.Operand}");
                break;

            case OpCode.LoadLocal:
                _sb.AppendLine($"    local.get {instr.Operand}");
                break;

            case OpCode.StoreLocal:
                _sb.AppendLine($"    local.set {instr.Operand}");
                break;

            case OpCode.AddInt:
                _sb.AppendLine("    i64.add");
                break;

            case OpCode.SubInt:
                _sb.AppendLine("    i64.sub");
                break;

            case OpCode.MulInt:
                _sb.AppendLine("    i64.mul");
                break;

            case OpCode.DivInt:
                _sb.AppendLine("    i64.div_s");
                break;

            case OpCode.AddFloat:
                _sb.AppendLine("    f64.add");
                break;

            case OpCode.SubFloat:
                _sb.AppendLine("    f64.sub");
                break;

            case OpCode.MulFloat:
                _sb.AppendLine("    f64.mul");
                break;

            case OpCode.DivFloat:
                _sb.AppendLine("    f64.div");
                break;

            case OpCode.Jump:
                _sb.AppendLine($"    br {instr.Operand}");
                break;

            case OpCode.JumpIfFalse:
                _sb.AppendLine("    i64.eqz");
                _sb.AppendLine($"    br_if {instr.Operand}");
                break;

            case OpCode.Call:
                _sb.AppendLine($"    call ${SanitizeWasmName($"func_{instr.Operand}")}");
                break;

            case OpCode.CallNative:
                _sb.AppendLine($"    call ${SanitizeWasmName($"native_{instr.Operand}")}");
                break;

            case OpCode.Return:
                _sb.AppendLine("    return");
                break;

            case OpCode.NegInt:
                _sb.AppendLine("    i64.neg");
                break;

            case OpCode.NegFloat:
                _sb.AppendLine("    f64.neg");
                break;

            case OpCode.Dup:
                _sb.AppendLine("    local.get 0");
                break;

            case OpCode.Pop:
                _sb.AppendLine("    drop");
                break;

            case OpCode.Halt:
                _sb.AppendLine("    unreachable");
                break;

            default:
                _sb.AppendLine($"    ;; 未实现的指令: {instr.OpCode} {instr.Operand}");
                break;
        }
    }

    #endregion

    #region Private Methods - Utilities

    private static string MangleName(string name)
    {
        return $"_gnosis_{SanitizeName(name)}";
    }

    private static string SanitizeName(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append($"_{((ushort)c):X4}_");
            }
        }

        return sb.ToString();
    }

    private static string SanitizeWasmName(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        return sb.ToString();
    }

    #endregion
}

public enum TargetPlatform
{
    X64,
    ARM64,
    WASM
}
