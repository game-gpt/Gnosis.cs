using System.Buffers.Binary;
using Gnosis.IR.Instruction;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 字节码模块适配器，将 BytecodeUnit 适配为 IModule
/// </summary>
public sealed class BytecodeModuleAdapter : IModule
{
    #region Fields

    private readonly BytecodeUnit _unit;
    private readonly Lazy<byte[]> _instructions;
    private readonly Lazy<IReadOnlyDictionary<string, object?>> _constants;
    private readonly Lazy<IReadOnlyList<ModuleFunctionInfo>> _functions;
    private readonly Lazy<IReadOnlyList<ModuleTypeInfo>> _types;

    #endregion

    #region Properties

    public string Name => _unit.ModuleName;

    public IReadOnlyList<byte> Instructions => _instructions.Value;

    public IReadOnlyDictionary<string, int> NativeBindings { get; } = new Dictionary<string, int>();

    public int EntryPoint => _unit.Functions.Count > 0 ? _unit.Functions[0].Instructions[0].OpCode == OpCode.Nop ? 1 : 0 : 0;

    public IReadOnlyDictionary<string, object?> Constants => _constants.Value;

    public IReadOnlyList<string> ExportedSymbols => _unit.Exports;

    public IReadOnlyList<string> ImportedSymbols => _unit.Imports;

    public int Version => 1;

    public bool IsValid => !string.IsNullOrEmpty(_unit.ModuleName) && _unit.Functions.Count > 0;

    public IReadOnlyList<ModuleFunctionInfo> Functions => _functions.Value;

    public IReadOnlyList<ModuleTypeInfo> Types => _types.Value;

    #endregion

    #region Constructors

    public BytecodeModuleAdapter(BytecodeUnit unit)
    {
        _unit = unit;
        _instructions = new Lazy<byte[]>(SerializeInstructions);
        _constants = new Lazy<IReadOnlyDictionary<string, object?>>(BuildConstants);
        _functions = new Lazy<IReadOnlyList<ModuleFunctionInfo>>(BuildFunctions);
        _types = new Lazy<IReadOnlyList<ModuleTypeInfo>>(() => []);
    }

    #endregion

    #region Private Methods

    private byte[] SerializeInstructions()
    {
        using var stream = new MemoryStream();

        foreach (var function in _unit.Functions)
        {
            foreach (var instruction in function.Instructions)
            {
                stream.WriteByte((byte)instruction.OpCode);
                WriteOperand(stream, instruction.OpCode, instruction.Operand);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// 根据指令类型写入变长操作数
    /// </summary>
    private static void WriteOperand(MemoryStream stream, OpCode opCode, long operand)
    {
        switch (opCode)
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
            case OpCode.CallModule:
            {
                Span<byte> bytes = stackalloc byte[8];
                BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)operand);
                BinaryPrimitives.WriteInt32LittleEndian(bytes[4..], 0);
                stream.Write(bytes);
                break;
            }

            case OpCode.LoadLocal:
            case OpCode.StoreLocal:
            case OpCode.LoadGlobal:
            case OpCode.StoreGlobal:
            case OpCode.LoadField:
            case OpCode.StoreField:
            case OpCode.NewObject:
            case OpCode.GetField:
            case OpCode.SetField:
            case OpCode.AddComponent:
            case OpCode.GetComponent:
            case OpCode.RemoveComponent:
            case OpCode.SetComponent:
            case OpCode.HasComponent:
            case OpCode.QueryWith:
            case OpCode.QueryWithout:
            case OpCode.DefineComponent:
            case OpCode.DefineSystem:
            case OpCode.SystemSchedule:
            case OpCode.PushString:
            case OpCode.NewArray:
            case OpCode.MakeClosure:
            case OpCode.IsType:
            case OpCode.TypeOf:
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

            case OpCode.QueryAll:
            case OpCode.QueryAny:
            {
                Span<byte> bytes = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)operand);
                stream.Write(bytes);
                break;
            }

            case OpCode.Halt:
            case OpCode.Nop:
            case OpCode.PushTrue:
            case OpCode.PushFalse:
            case OpCode.PushNull:
            case OpCode.Pop:
            case OpCode.Dup:
            case OpCode.AddInt:
            case OpCode.SubInt:
            case OpCode.MulInt:
            case OpCode.DivInt:
            case OpCode.AddFloat:
            case OpCode.SubFloat:
            case OpCode.MulFloat:
            case OpCode.DivFloat:
            case OpCode.NegInt:
            case OpCode.NegFloat:
            case OpCode.EqualInt:
            case OpCode.NotEqualInt:
            case OpCode.LessInt:
            case OpCode.GreaterInt:
            case OpCode.LessEqualInt:
            case OpCode.GreaterEqualInt:
            case OpCode.EqualFloat:
            case OpCode.NotEqualFloat:
            case OpCode.LessFloat:
            case OpCode.GreaterFloat:
            case OpCode.LessEqualFloat:
            case OpCode.GreaterEqualFloat:
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Not:
            case OpCode.Return:
            case OpCode.SpawnEntity:
            case OpCode.DestroyEntity:
            case OpCode.WorldUpdate:
            case OpCode.ConcatString:
            case OpCode.StringLength:
            case OpCode.StringGetChar:
            case OpCode.ArrayGet:
            case OpCode.ArraySet:
            case OpCode.ArrayLength:
            case OpCode.GetUpvalue:
            case OpCode.SetUpvalue:
            case OpCode.IsNull:
                break;

            default:
            {
                Span<byte> bytes = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)operand);
                stream.Write(bytes);
                break;
            }
        }
    }

    private IReadOnlyDictionary<string, object?> BuildConstants()
    {
        var dict = new Dictionary<string, object?>();

        for (var i = 0; i < _unit.Constants.Count; i++)
        {
            dict[i.ToString()] = _unit.Constants[i];
        }

        return dict;
    }

    private IReadOnlyList<ModuleFunctionInfo> BuildFunctions()
    {
        var result = new List<ModuleFunctionInfo>();
        var offset = 0;

        foreach (var function in _unit.Functions)
        {
            result.Add(new ModuleFunctionInfo(
                function.Name,
                function.ParameterCount,
                function.LocalCount,
                offset));

            foreach (var instruction in function.Instructions)
            {
                offset += 1 + GetOperandSize(instruction.OpCode);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指令操作数的字节大小
    /// </summary>
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
                or OpCode.SetComponent or OpCode.HasComponent
                or OpCode.QueryWith or OpCode.QueryWithout
                or OpCode.DefineComponent or OpCode.DefineSystem or OpCode.SystemSchedule
                or OpCode.PushString or OpCode.NewArray or OpCode.MakeClosure
                or OpCode.IsType or OpCode.TypeOf or OpCode.QueryAll or OpCode.QueryAny => 4,
            OpCode.CallModule => 8,
            _ => 0
        };
    }

    #endregion
}
