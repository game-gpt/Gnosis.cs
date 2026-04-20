namespace Gnosis.Interpreter.VM;

public interface INativeFunction
{
    int Id { get; }
    string Name { get; }
    int ParameterCount { get; }
    object? Execute(IVMState vm, object?[] args);
}
