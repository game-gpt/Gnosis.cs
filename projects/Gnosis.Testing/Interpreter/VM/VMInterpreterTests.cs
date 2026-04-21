using System;
using System.Collections.Generic;
using System.IO;
using Gnosis.Interpreter.IR;
using Gnosis.Interpreter.VM;
using NUnit.Framework;

namespace Gnosis.Testing.Interpreter.VM;

/// <summary>
/// 虚拟机解释器单元测试
/// </summary>
[TestFixture]
public class VMInterpreterTests : TestBase
{
    #region 辅助类

    private class MockModule : IModule
    {
        public string Name { get; }
        public IReadOnlyList<byte> Instructions { get; }
        public IReadOnlyDictionary<string, int> NativeBindings { get; } = new Dictionary<string, int>();
        public int EntryPoint => 0;
        public IReadOnlyDictionary<string, object?> Constants { get; }
        public IReadOnlyList<string> ExportedSymbols { get; } = Array.Empty<string>();
        public IReadOnlyList<string> ImportedSymbols { get; } = Array.Empty<string>();
        public int Version => 1;
        public bool IsValid => true;

        public MockModule(string name, byte[] instructions, IReadOnlyDictionary<string, object?>? constants = null)
        {
            Name = name;
            Instructions = instructions;
            Constants = constants ?? new Dictionary<string, object?>();
        }
    }

    private class MockNativeFunction : INativeFunction
    {
        public int Id { get; }
        public string Name { get; }
        public int ParameterCount { get; }
        private readonly Func<object?[]?, object?> _implementation;

        public object? Execute(IVMState vm, object?[] args)
        {
            var result = _implementation(args);
            if (result is not null)
            {
                vm.Push(result);
            }
            return result;
        }

        public MockNativeFunction(int id, string name, int parameterCount, Func<object?[]?, object?>? implementation = null)
        {
            Id = id;
            Name = name;
            ParameterCount = parameterCount;
            _implementation = implementation ?? (_ => null);
        }
    }

    #endregion

    #region 辅助方法

    private static void WriteOpCode(MemoryStream ms, OpCode op)
    {
        ms.WriteByte((byte)op);
    }

    private static void WriteInt32(MemoryStream ms, int value)
    {
        ms.Write(BitConverter.GetBytes(value), 0, 4);
    }

    private static void WriteFloat(MemoryStream ms, float value)
    {
        ms.Write(BitConverter.GetBytes(value), 0, 4);
    }

    private static void WriteByte(MemoryStream ms, byte value)
    {
        ms.WriteByte(value);
    }

    private static VMInterpreter CreateInterpreter(byte[] instructions, IReadOnlyDictionary<string, object?>? constants = null)
    {
        var state = new VMState();
        var registry = new NativeFunctionRegistry();
        var module = new MockModule("test", instructions, constants);
        state.LoadModule(module);
        return new VMInterpreter(state, registry);
    }

    #endregion

    #region 基本执行测试

    [Test]
    public void Run_NoModule_ThrowsModuleNotFound()
    {
        var state = new VMState();
        var registry = new NativeFunctionRegistry();
        var interpreter = new VMInterpreter(state, registry);

        Assert.Throws<VMModuleNotFoundException>(() => interpreter.Run());
    }

    [Test]
    public void Halt_StopsExecution()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.IsRunning, Is.False);
    }

    [Test]
    public void Nop_DoesNothing()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Nop);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    #endregion

    #region 常量加载测试

    [Test]
    public void PushInt8_PushesValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt8);
        WriteByte(ms, 42);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void PushInt32_PushesValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void PushFloat32_PushesValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushFloat32);
        WriteFloat(ms, 3.14f);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo((double)3.14f).Within(1e-6));
    }

    #endregion

    #region 栈操作测试

    [Test]
    public void Pop_RemovesTopValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 10);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 20);
        WriteOpCode(ms, OpCode.Pop);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(10L));
    }

    [Test]
    public void Dup_CopiesTopValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Dup);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    #endregion

    #region 整数算术测试

    [Test]
    public void AddInt_ComputesSum()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 58);
        WriteOpCode(ms, OpCode.AddInt);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(100L));
    }

    [Test]
    public void SubInt_ComputesDifference()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 100);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 40);
        WriteOpCode(ms, OpCode.SubInt);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(60L));
    }

    [Test]
    public void MulInt_ComputesProduct()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 6);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 7);
        WriteOpCode(ms, OpCode.MulInt);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void DivInt_ComputesQuotient()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 100);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 4);
        WriteOpCode(ms, OpCode.DivInt);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(25L));
    }

    [Test]
    public void DivInt_ByZero_ThrowsException()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 10);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 0);
        WriteOpCode(ms, OpCode.DivInt);

        var interpreter = CreateInterpreter(ms.ToArray());

        Assert.Throws<VMDivideByZeroException>(() => interpreter.Run());
    }

    [Test]
    public void NegInt_NegatesValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.NegInt);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(-42L));
    }

    #endregion

    #region 浮点算术测试

    [Test]
    public void AddFloat_ComputesSum()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushFloat32);
        WriteFloat(ms, 1.5f);
        WriteOpCode(ms, OpCode.PushFloat32);
        WriteFloat(ms, 2.5f);
        WriteOpCode(ms, OpCode.AddFloat);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(4.0).Within(1e-6));
    }

    #endregion

    #region 控制流测试

    [Test]
    public void Jump_ChangesIP()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Jump);
        WriteInt32(ms, 15);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 99);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void JumpIfFalse_WhenFalse_Jumps()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 0);
        WriteOpCode(ms, OpCode.JumpIfFalse);
        WriteInt32(ms, 16);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 99);
        WriteOpCode(ms, OpCode.Halt);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void JumpIfFalse_WhenTrue_DoesNotJump()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 1);
        WriteOpCode(ms, OpCode.JumpIfFalse);
        WriteInt32(ms, 15);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void JumpIfTrue_WhenTrue_Jumps()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 1);
        WriteOpCode(ms, OpCode.JumpIfTrue);
        WriteInt32(ms, 16);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 99);
        WriteOpCode(ms, OpCode.Halt);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    #endregion

    #region 函数调用测试

    [Test]
    public void Call_Return_FunctionCall()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.Call);
        WriteInt32(ms, 11);
        WriteOpCode(ms, OpCode.Halt);
        WriteOpCode(ms, OpCode.NegInt);
        WriteOpCode(ms, OpCode.Return);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(-42L));
    }

    [Test]
    public void CallNative_ExecutesFunction()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 10);
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 20);
        WriteOpCode(ms, OpCode.CallNative);
        WriteInt32(ms, 1);
        WriteOpCode(ms, OpCode.Halt);

        var state = new VMState();
        var registry = new NativeFunctionRegistry();
        registry.Register(new MockNativeFunction(1, "add", 2, args => (long)args[0]! + (long)args[1]!));
        var module = new MockModule("test", ms.ToArray());
        state.LoadModule(module);
        var interpreter = new VMInterpreter(state, registry);

        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(30L));
    }

    #endregion

    #region 变量访问测试

    [Test]
    public void LoadLocal_StoreLocal_RoundTrip()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 42);
        WriteOpCode(ms, OpCode.StoreLocal);
        WriteInt32(ms, 0);
        WriteOpCode(ms, OpCode.LoadLocal);
        WriteInt32(ms, 0);
        WriteOpCode(ms, OpCode.Halt);

        var state = new VMState();
        var registry = new NativeFunctionRegistry();
        var module = new MockModule("test", ms.ToArray());
        state.LoadModule(module);
        state.StackInternal.PushFrame(0, 0, 4);
        var interpreter = new VMInterpreter(state, registry);

        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(42L));
    }

    [Test]
    public void LoadGlobal_LoadsConstant()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.LoadGlobal);
        WriteInt32(ms, 0);
        WriteOpCode(ms, OpCode.Halt);

        var constants = new Dictionary<string, object?> { ["0"] = 99L };
        var interpreter = CreateInterpreter(ms.ToArray(), constants);
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(99L));
    }

    #endregion

    #region 实体操作测试

    [Test]
    public void SpawnEntity_WithoutWorld_PushesZero()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.SpawnEntity);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.Pop(), Is.EqualTo(0L));
    }

    [Test]
    public void DestroyEntity_WithoutWorld_PopsValue()
    {
        using var ms = new MemoryStream();
        WriteOpCode(ms, OpCode.PushInt32);
        WriteInt32(ms, 1);
        WriteOpCode(ms, OpCode.DestroyEntity);
        WriteOpCode(ms, OpCode.Halt);

        var interpreter = CreateInterpreter(ms.ToArray());
        interpreter.Run();

        Assert.That(interpreter.State.StackInternal.Count, Is.EqualTo(0));
    }

    #endregion
}
