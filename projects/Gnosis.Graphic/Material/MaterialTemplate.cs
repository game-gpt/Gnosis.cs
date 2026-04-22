namespace Gnosis.Graphic.Material;

public sealed class MaterialTemplate
{
    private readonly Dictionary<string, MaterialPropertyDescriptor> _properties = [];
    private readonly List<MaterialPropertyDescriptor> _orderedProperties = [];

    public string Name { get; }
    public IReadOnlyList<MaterialPropertyDescriptor> Properties => _orderedProperties;

    public MaterialTemplate(string name)
    {
        Name = name;
    }

    public MaterialTemplate AddProperty(
        string name,
        MaterialPropertyType type,
        object? defaultValue = null,
        uint binding = 0,
        uint group = 0)
    {
        var descriptor = new MaterialPropertyDescriptor(name, type, defaultValue, binding, group);
        _properties[name] = descriptor;
        _orderedProperties.Add(descriptor);
        return this;
    }

    public MaterialPropertyDescriptor? GetProperty(string name)
    {
        return _properties.GetValueOrDefault(name);
    }

    public bool HasProperty(string name)
    {
        return _properties.ContainsKey(name);
    }

    public MaterialInstance CreateInstance()
    {
        return new MaterialInstance(this);
    }
}
