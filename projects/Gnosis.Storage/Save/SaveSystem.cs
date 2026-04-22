using Gnosis.Storage.Provider;

namespace Gnosis.Storage.Save;

public sealed class SaveSystem
{
    #region 字段

    private readonly IStorageProvider _provider;
    private readonly Dictionary<string, SaveSlot> _slots = new();

    #endregion

    #region 属性

    public int SlotCount => _slots.Count;

    public bool AutoSaveEnabled { get; set; }

    public float AutoSaveInterval { get; set; } = 300f;

    #endregion

    #region 构造函数

    public SaveSystem(IStorageProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    #endregion

    #region 公开方法

    public SaveSlot CreateSlot(int index, string name)
    {
        var slot = new SaveSlot(index, name);
        _slots[$"slot_{index}"] = slot;
        return slot;
    }

    public SaveSlot? GetSlot(int index)
    {
        return _slots.TryGetValue($"slot_{index}", out var slot) ? slot : null;
    }

    public void DeleteSlot(int index)
    {
        _slots.Remove($"slot_{index}");
    }

    public async Task SaveAsync(int slotIndex)
    {
        var slot = GetSlot(slotIndex);

        if (slot is null)
        {
            return;
        }

        var data = slot.Serialize();
        await _provider.SaveAsync($"save_slot_{slotIndex}", data);
    }

    public async Task LoadAsync(int slotIndex)
    {
        var data = await _provider.LoadAsync($"save_slot_{slotIndex}");

        if (data is not null)
        {
            var slot = GetSlot(slotIndex);

            if (slot is not null)
            {
                slot.Deserialize(data);
            }
        }
    }

    #endregion
}
