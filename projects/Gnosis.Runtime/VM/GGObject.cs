namespace Gnosis.Runtime.VM;

public class GGObject : IGCObject
{
    private readonly Dictionary<string, GGValue> _fields = new();
    public string TypeName { get; }
    public int ObjectId { get; set; }
    public bool IsMarked { get; set; }

    public GGObject(string typeName)
    {
        TypeName = typeName;
        IsMarked = false;
    }

    public void SetField(string name, GGValue value)
    {
        _fields[name] = value;
    }

    public GGValue GetField(string name)
    {
        return _fields.GetValueOrDefault(name);
    }

    public bool HasField(string name)
    {
        return _fields.ContainsKey(name);
    }

    public IReadOnlyDictionary<string, GGValue> Fields => _fields;

    public IEnumerable<IGCObject?> GetGCReferences()
    {
        foreach (var field in _fields.Values)
        {
            if (field.Reference is IGCObject gcObj)
            {
                yield return gcObj;
            }
        }
    }
}
