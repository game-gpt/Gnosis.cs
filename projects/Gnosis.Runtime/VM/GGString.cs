namespace Gnosis.Runtime.VM;

public class GGString : IGCObject
{
    public string Value { get; }
    public int ObjectId { get; set; }
    public bool IsMarked { get; set; }

    public GGString(string value)
    {
        Value = value;
        IsMarked = false;
        ObjectId = 0;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is GGString other && Value == other.Value;

    public IEnumerable<IGCObject?> GetGCReferences()
    {
        return Array.Empty<IGCObject?>();
    }
}

public class StringPool
{
    private readonly Dictionary<string, GGString> _pool = new();

    public GGString Intern(string value)
    {
        if (_pool.TryGetValue(value, out var existing))
        {
            return existing;
        }

        var str = new GGString(value);
        _pool[value] = str;
        return str;
    }

    public GGString? Lookup(string value)
    {
        return _pool.GetValueOrDefault(value);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}
