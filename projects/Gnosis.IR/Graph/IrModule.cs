namespace Gnosis.IR.Graph;

public sealed class IrModule
{
    #region Properties

    public string Name { get; }

    public IReadOnlyList<IrFunction> Functions => _functions;

    public IReadOnlyList<IrType> StructTypes => _structTypes;

    public IReadOnlyList<string> Imports => _imports;

    public IReadOnlyList<string> Exports => _exports;

    #endregion

    #region Fields

    private readonly List<IrFunction> _functions = [];
    private readonly List<IrType> _structTypes = [];
    private readonly List<string> _imports = [];
    private readonly List<string> _exports = [];

    #endregion

    #region Constructors

    public IrModule(string name)
    {
        Name = name;
    }

    #endregion

    #region Public Methods - Function Management

    public IrFunction CreateFunction(string name, IrType returnType,
        IReadOnlyList<(string Name, IrType Type)>? parameters = null,
        IReadOnlyList<string>? attributes = null)
    {
        var function = new IrFunction(name, returnType, parameters, attributes);
        function.Parent = this;
        _functions.Add(function);
        return function;
    }

    public void AddFunction(IrFunction function)
    {
        function.Parent = this;
        _functions.Add(function);
    }

    public void RemoveFunction(IrFunction function)
    {
        function.Parent = null;
        _functions.Remove(function);
    }

    public IrFunction? FindFunction(string name)
    {
        return _functions.Find(f => f.Name == name);
    }

    #endregion

    #region Public Methods - Type Management

    public void AddStructType(IrType structType)
    {
        if (structType.Kind != IrTypeKind.Struct)
        {
            throw new ArgumentException("类型必须是结构体类型", nameof(structType));
        }
        _structTypes.Add(structType);
    }

    #endregion

    #region Public Methods - Import/Export

    public void AddImport(string moduleName)
    {
        if (!_imports.Contains(moduleName))
        {
            _imports.Add(moduleName);
        }
    }

    public void AddExport(string symbolName)
    {
        if (!_exports.Contains(symbolName))
        {
            _exports.Add(symbolName);
        }
    }

    #endregion

    #region Public Methods - Text IR

    public string ToIrText()
    {
        var writer = new StringWriter();
        WriteIrText(writer);
        return writer.ToString();
    }

    public void WriteIrText(TextWriter writer)
    {
        writer.WriteLine($"module {Name};");
        writer.WriteLine();

        foreach (var import in _imports)
        {
            writer.WriteLine($"import {import};");
        }

        if (_imports.Count > 0)
        {
            writer.WriteLine();
        }

        foreach (var structType in _structTypes)
        {
            writer.WriteLine($"struct {structType.Name} {{");
            foreach (var (name, type) in structType.Fields)
            {
                writer.WriteLine($"  {name}: {type},");
            }
            writer.WriteLine("}");
            writer.WriteLine();
        }

        foreach (var function in _functions)
        {
            function.WriteIrText(writer);
            writer.WriteLine();
        }
    }

    #endregion

    public override string ToString()
    {
        return $"module {Name}";
    }
}
