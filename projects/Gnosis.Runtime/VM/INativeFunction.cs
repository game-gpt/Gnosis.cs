namespace Gnosis.Runtime.VM;

public interface INativeFunction
{
    int Id { get; }
    string Name { get; }
    int ParameterCount { get; }
    GGValue Execute(IVMState vm, GGValue[] args);
}
