namespace Gnosis.Runtime.VM;

public class MemoryManager
{
    #region Fields

    private readonly Dictionary<int, IGCObject> _heap = new();
    private readonly List<IGCObject> _roots = new();
    private int _nextId;
    private long _allocatedBytes;
    private const long DefaultGcThreshold = 4 * 1024 * 1024;
    private long _gcThreshold;

    #endregion

    #region Constructors

    public MemoryManager()
    {
        _nextId = 0;
        _allocatedBytes = 0;
        _gcThreshold = DefaultGcThreshold;
    }

    #endregion

    #region Properties

    public int ObjectCount => _heap.Count;
    public long AllocatedBytes => _allocatedBytes;
    public long GcThreshold
    {
        get => _gcThreshold;
        set => _gcThreshold = value;
    }

    #endregion

    #region Allocation

    public int Allocate(IGCObject obj)
    {
        var id = _nextId++;
        obj.ObjectId = id;
        _heap[id] = obj;
        _allocatedBytes += EstimateSize(obj);

        if (_allocatedBytes >= _gcThreshold)
        {
            Collect();
        }

        return id;
    }

    public IGCObject? GetObject(int objectId)
    {
        return _heap.GetValueOrDefault(objectId);
    }

    public T? GetObject<T>(int objectId) where T : class, IGCObject
    {
        return _heap.TryGetValue(objectId, out var obj) ? obj as T : null;
    }

    public bool TryGetObject(int objectId, out IGCObject? obj)
    {
        return _heap.TryGetValue(objectId, out obj);
    }

    public void Free(int objectId)
    {
        if (_heap.TryGetValue(objectId, out var obj))
        {
            _allocatedBytes -= EstimateSize(obj);
            _heap.Remove(objectId);
        }
    }

    #endregion

    #region Roots

    public void AddRoot(IGCObject obj)
    {
        if (!_roots.Contains(obj))
        {
            _roots.Add(obj);
        }
    }

    public void RemoveRoot(IGCObject obj)
    {
        _roots.Remove(obj);
    }

    public void ClearRoots()
    {
        _roots.Clear();
    }

    public void SetRootsFromStack(object?[] stackValues, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (stackValues[i] is IGCObject gcObj)
            {
                AddRoot(gcObj);
            }
        }
    }

    public void SetRootsFromLocals(object?[] locals)
    {
        foreach (var local in locals)
        {
            if (local is IGCObject gcObj)
            {
                AddRoot(gcObj);
            }
        }
    }

    #endregion

    #region GC

    public void Collect()
    {
        foreach (var obj in _heap.Values)
        {
            obj.IsMarked = false;
        }

        foreach (var root in _roots)
        {
            Mark(root);
        }

        Sweep();
    }

    private void Mark(IGCObject obj)
    {
        if (obj.IsMarked)
        {
            return;
        }

        obj.IsMarked = true;

        foreach (var reference in obj.GetGCReferences())
        {
            if (reference is not null)
            {
                Mark(reference);
            }
        }
    }

    private void Sweep()
    {
        var unreachableIds = _heap
            .Where(kvp => !kvp.Value.IsMarked)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in unreachableIds)
        {
            _allocatedBytes -= EstimateSize(_heap[id]);
            _heap.Remove(id);
        }
    }

    #endregion

    #region Helper

    private static long EstimateSize(IGCObject obj)
    {
        return obj switch
        {
            GGString => 64,
            GGArray arr => 48 + arr.Capacity * 8,
            GGObject => 128,
            GGStruct => 64,
            GGClosure => 48,
            _ => 64
        };
    }

    public void Clear()
    {
        _heap.Clear();
        _roots.Clear();
        _nextId = 0;
        _allocatedBytes = 0;
    }

    #endregion
}
