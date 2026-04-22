namespace Gnosis.Save;

public interface ISaveSlot
{
    int Index { get; }
    string Name { get; set; }
    bool IsEmpty { get; }
    long Timestamp { get; }
    ISaveData? Data { get; }
    void Save(ISaveData data);
    ISaveData Load();
    void Delete();
}
