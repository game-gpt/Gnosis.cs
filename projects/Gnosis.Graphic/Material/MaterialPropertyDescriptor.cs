namespace Gnosis.Graphic.Material;

public sealed class MaterialPropertyDescriptor
{
    public string Name { get; }
    public MaterialPropertyType Type { get; }
    public object? DefaultValue { get; }
    public uint Binding { get; }
    public uint Group { get; }

    public MaterialPropertyDescriptor(
        string name,
        MaterialPropertyType type,
        object? defaultValue = null,
        uint binding = 0,
        uint group = 0)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Binding = binding;
        Group = group;
    }
}
