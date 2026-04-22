namespace Gnosis.Runtime.VM;

public class GGStruct : IGCObject
{
    private readonly object?[] _fields;
    public string TypeName { get; }
    public string[] FieldNames { get; }
    public int ObjectId { get; set; }
    public bool IsMarked { get; set; }

    public GGStruct(string typeName, string[] fieldNames)
    {
        TypeName = typeName;
        FieldNames = fieldNames;
        _fields = new object?[fieldNames.Length];
        IsMarked = false;
        ObjectId = 0;
    }

    public object? GetField(int index)
    {
        if (index < 0 || index >= _fields.Length)
        {
            throw new VMIndexOutOfBoundsException(index, _fields.Length);
        }
        return _fields[index];
    }

    public void SetField(int index, object? value)
    {
        if (index < 0 || index >= _fields.Length)
        {
            throw new VMIndexOutOfBoundsException(index, _fields.Length);
        }
        _fields[index] = value;
    }

    public int GetFieldIndex(string name)
    {
        return Array.IndexOf(FieldNames, name);
    }

    public IEnumerable<IGCObject?> GetGCReferences()
    {
        return _fields.OfType<IGCObject>();
    }

    public IEnumerable<object?> GetReferences()
    {
        return _fields.Where(v => v is GGObject or GGArray or GGString or GGStruct or GGClosure);
    }
}
