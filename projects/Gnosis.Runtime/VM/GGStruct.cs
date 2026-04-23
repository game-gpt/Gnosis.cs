namespace Gnosis.Runtime.VM;

public class GGStruct : IGCObject
{
    private readonly GGValue[] _fields;
    public string TypeName { get; }
    public string[] FieldNames { get; }
    public int ObjectId { get; set; }
    public bool IsMarked { get; set; }

    public GGStruct(string typeName, string[] fieldNames)
    {
        TypeName = typeName;
        FieldNames = fieldNames;
        _fields = new GGValue[fieldNames.Length];
        IsMarked = false;
        ObjectId = 0;
    }

    public GGValue GetField(int index)
    {
        if (index < 0 || index >= _fields.Length)
        {
            throw new VMIndexOutOfBoundsException(index, _fields.Length);
        }
        return _fields[index];
    }

    public void SetField(int index, GGValue value)
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
        foreach (var field in _fields)
        {
            if (field.Reference is IGCObject gcObj)
            {
                yield return gcObj;
            }
        }
    }
}
