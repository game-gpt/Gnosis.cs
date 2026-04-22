namespace Gnosis.Runtime.VM;

public interface IGCObject
{
    int ObjectId { get; set; }
    bool IsMarked { get; set; }
    IEnumerable<IGCObject?> GetGCReferences();
}
