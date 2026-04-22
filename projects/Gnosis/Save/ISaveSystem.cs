namespace Gnosis.Save;

public interface ISaveSystem
{
    int SlotCount { get; }
    bool IsAutoSaveEnabled { get; set; }
    float AutoSaveInterval { get; set; }
    bool IsEncrypted { get; set; }
    ISaveSlot GetSlot(int index);
    void Save(int slotIndex);
    void Load(int slotIndex);
    void DeleteSave(int slotIndex);
    bool HasSave(int slotIndex);
    void SetSerializer(ISaveSerializer serializer);
    void Update(float delta);
}
