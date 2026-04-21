using Gnosis.Interpreter.IR;

namespace Gnosis.Interpreter.VM;

public sealed class BytecodeModuleAdapter : IModule
{
    #region Fields

    private readonly BytecodeUnit _unit;
    private readonly Lazy<byte[]> _instructions;
    private readonly Lazy<IReadOnlyDictionary<string, object?>> _constants;

    #endregion

    #region Properties

    public string Name => _unit.ModuleName;

    public IReadOnlyList<byte> Instructions => _instructions.Value;

    public IReadOnlyDictionary<string, int> NativeBindings { get; } = new Dictionary<string, int>();

    public int EntryPoint => 0;

    public IReadOnlyDictionary<string, object?> Constants => _constants.Value;

    public IReadOnlyList<string> ExportedSymbols => _unit.Exports;

    public IReadOnlyList<string> ImportedSymbols => _unit.Imports;

    public int Version => 1;

    public bool IsValid => !string.IsNullOrEmpty(_unit.ModuleName) && _unit.Functions.Count > 0;

    #endregion

    #region Constructors

    public BytecodeModuleAdapter(BytecodeUnit unit)
    {
        _unit = unit;
        _instructions = new Lazy<byte[]>(SerializeInstructions);
        _constants = new Lazy<IReadOnlyDictionary<string, object?>>(BuildConstants);
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
                var operandBytes = BitConverter.GetBytes(instruction.Operand);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(operandBytes);
                }
                stream.Write(operandBytes, 0, operandBytes.Length);
            }
        }
        return stream.ToArray();
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

    #endregion
}
