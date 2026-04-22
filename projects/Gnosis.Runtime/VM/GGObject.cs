namespace Gnosis.Runtime.VM;

public class GGObject : IGCObject
{
    private readonly Dictionary<string, object?> _fields = new();
    public string TypeName { get; }
    public int ObjectId { get; set; }
    public bool IsMarked { get; set; }

    public GGObject(string typeName)
    {
        TypeName = typeName;
        IsMarked = false;
    }

    public void SetField(string name, object? value)
    {
        _fields[name] = value;
    }

    public object? GetField(string name)
    {
        return _fields.GetValueOrDefault(name);
    }

    public bool HasField(string name)
    {
        return _fields.ContainsKey(name);
    }

    public IReadOnlyDictionary<string, object?> Fields => _fields;

    public IEnumerable<IGCObject?> GetGCReferences()
    {
        return _fields.Values.OfType<IGCObject>();
    }
}
