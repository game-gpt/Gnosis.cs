using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Gnosis.ECS.World;
using Gnosis.IR.Instruction;
using Gnosis.Runtime.VM;

namespace Gnosis.Benchmarks;

[Config(typeof(VMInstructionConfig))]
[MemoryDiagnoser]
[RankColumn]
public class VMInstructionBenchmarks
{
    private class VMInstructionConfig : ManualConfig
    {
        public VMInstructionConfig()
        {
            AddJob(Job.ShortRun
                .WithWarmupCount(2)
                .WithIterationCount(8));
            AddColumn(StatisticColumn.P95);
        }
    }

    private VMState _state = null!;
    private VMInterpreter _vm = null!;
    private NativeFunctionRegistry _registry = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _state = new VMState();
        _registry = new NativeFunctionRegistry();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _state = new VMState();
        _registry = new NativeFunctionRegistry();
    }

    #region 字节码构建辅助

    private static byte[] BuildBytecode(params (OpCode op, int operand)[] instructions)
    {
        using var stream = new MemoryStream();

        foreach (var (op, operand) in instructions)
        {
            stream.WriteByte((byte)op);
            WriteOperand(stream, op, operand);
        }

        return stream.ToArray();
    }

    private static void WriteOperand(MemoryStream stream, OpCode op, long operand)
    {
        switch (op)
        {
            case OpCode.PushInt8:
                stream.WriteByte((byte)operand);
                break;
            case OpCode.PushInt16:
            {
                Span<byte> bytes = stackalloc byte[2];
                BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)operand);
                stream.Write(bytes);
                break;
            }
            case OpCode.PushInt32:
            case OpCode.Jump:
            case OpCode.JumpIfTrue:
            case OpCode.JumpIfFalse:
            case OpCode.Call:
            case OpCode.CallNative:
            case OpCode.LoadLocal:
            case OpCode.StoreLocal:
            case OpCode.LoadGlobal:
            case OpCode.StoreGlobal:
            case OpCode.NewObject:
            case OpCode.NewArray:
            case OpCode.MakeClosure:
            case OpCode.AddComponent:
            case OpCode.GetComponent:
            case OpCode.RemoveComponent:
            case OpCode.QueryAll:
            case OpCode.QueryAny:
            {
                Span<byte> bytes = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)operand);
                stream.Write(bytes);
                break;
            }
            case OpCode.PushInt64:
            {
                Span<byte> bytes = stackalloc byte[8];
                BinaryPrimitives.WriteInt64LittleEndian(bytes, operand);
                stream.Write(bytes);
                break;
            }
            case OpCode.PushFloat32:
            {
                Span<byte> bytes = stackalloc byte[4];
                BinaryPrimitives.WriteSingleLittleEndian(bytes, (float)BitConverter.Int64BitsToDouble(operand));
                stream.Write(bytes);
                break;
            }
            case OpCode.PushFloat64:
            {
                Span<byte> bytes = stackalloc byte[8];
                BinaryPrimitives.WriteDoubleLittleEndian(bytes, BitConverter.Int64BitsToDouble(operand));
                stream.Write(bytes);
                break;
            }
        }
    }

    private IModule CreateTestModule(byte[] bytecode)
    {
        var unit = new BytecodeUnit(
            "benchmark",
            [],
            [new BytecodeFunction("main", 0, 0,
                bytecode.Select(b => new BytecodeInstruction((OpCode)b, 0)).ToList())],
            [],
            [],
            []);
        return new BytecodeModuleAdapter(unit);
    }

    #endregion

    #region 整数算术指令

    [Params(100, 1000)]
    public int InstructionCount { get; set; }

    [Benchmark]
    public void VM_AddInt_Loop()
    {
        var instructions = new List<(OpCode, int)>
        {
            (OpCode.PushInt32, 1),
            (OpCode.PushInt32, 2)
        };

        for (var i = 0; i < InstructionCount; i++)
        {
            instructions.Add((OpCode.AddInt, 0));
            instructions.Add((OpCode.PushInt32, 1));
        }

        instructions.Add((OpCode.Pop, 0));
        instructions.Add((OpCode.Halt, 0));

        var bytecode = BuildBytecode(instructions.ToArray());
        var module = CreateTestModule(bytecode);

        _state.LoadModule(module);
        _vm = new VMInterpreter(_state, _registry);
        _vm.Run();
        _state.Reset();
    }

    [Benchmark]
    public void VM_PushPop_Loop()
    {
        var instructions = new List<(OpCode, int)>();

        for (var i = 0; i < InstructionCount; i++)
        {
            instructions.Add((OpCode.PushInt32, i));
        }

        for (var i = 0; i < InstructionCount; i++)
        {
            instructions.Add((OpCode.Pop, 0));
        }

        instructions.Add((OpCode.Halt, 0));

        var bytecode = BuildBytecode(instructions.ToArray());
        var module = CreateTestModule(bytecode);

        _state.LoadModule(module);
        _vm = new VMInterpreter(_state, _registry);
        _vm.Run();
        _state.Reset();
    }

    #endregion

    #region ECS 指令

    [Benchmark]
    public void VM_SpawnEntity_Loop()
    {
        var instructions = new List<(OpCode, int)>();

        for (var i = 0; i < InstructionCount; i++)
        {
            instructions.Add((OpCode.SpawnEntity, 0));
        }

        for (var i = 0; i < InstructionCount; i++)
        {
            instructions.Add((OpCode.Pop, 0));
        }

        instructions.Add((OpCode.Halt, 0));

        var bytecode = BuildBytecode(instructions.ToArray());
        var module = CreateTestModule(bytecode);

        var world = new World();
        _state.LoadModule(module);
        _vm = new VMInterpreter(_state, _registry, world);
        _vm.Run();
        _state.Reset();
    }

    #endregion

    #region 栈操作

    [Benchmark]
    public void Stack_PushPop_Raw()
    {
        var stack = new VMStack(1024);
        var val = GGValue.FromInt(42);

        for (var i = 0; i < InstructionCount; i++)
        {
            stack.Push(val);
        }

        for (var i = 0; i < InstructionCount; i++)
        {
            stack.Pop();
        }
    }

    [Benchmark]
    public void Stack_Dup_Raw()
    {
        var stack = new VMStack(2048);
        stack.Push(GGValue.FromInt(42));

        for (var i = 0; i < InstructionCount; i++)
        {
            stack.Dup();
        }
    }

    #endregion
}
