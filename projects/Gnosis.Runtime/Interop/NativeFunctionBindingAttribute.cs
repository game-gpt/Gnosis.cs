namespace Gnosis.Runtime.Interop;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NativeFunctionBindingAttribute : Attribute
{
    public string Name { get; }
    public int Id { get; }

    public NativeFunctionBindingAttribute(string name, int id)
    {
        Name = name;
        Id = id;
    }
}
