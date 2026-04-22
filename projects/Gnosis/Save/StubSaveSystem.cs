namespace Gnosis.Save;

public class StubSaveSystem : ISaveSystem
{
    public int SlotCount => throw new NotImplementedException("存档系统尚未实现");
    public bool IsAutoSaveEnabled { get => throw new NotImplementedException("存档系统尚未实现"); set => throw new NotImplementedException("存档系统尚未实现"); }
    public float AutoSaveInterval { get => throw new NotImplementedException("存档系统尚未实现"); set => throw new NotImplementedException("存档系统尚未实现"); }
    public bool IsEncrypted { get => throw new NotImplementedException("存档系统尚未实现"); set => throw new NotImplementedException("存档系统尚未实现"); }

    public void DeleteSave(int slotIndex) { throw new NotImplementedException("存档系统尚未实现"); }
    public ISaveSlot GetSlot(int index) { throw new NotImplementedException("存档系统尚未实现"); }
    public bool HasSave(int slotIndex) { throw new NotImplementedException("存档系统尚未实现"); }
    public void Load(int slotIndex) { throw new NotImplementedException("存档系统尚未实现"); }
    public void Save(int slotIndex) { throw new NotImplementedException("存档系统尚未实现"); }
    public void SetSerializer(ISaveSerializer serializer) { throw new NotImplementedException("存档系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("存档系统尚未实现"); }
}
