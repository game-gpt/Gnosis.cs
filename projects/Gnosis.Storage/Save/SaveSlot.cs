namespace Gnosis.Storage.Save;

public sealed class SaveSlot
{
    #region 属性

    public int Index { get; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; set; }

    public Dictionary<string, string> Metadata { get; } = new();

    #endregion

    #region 构造函数

    public SaveSlot(int index, string name)
    {
        Index = index;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region 序列化

    public byte[] Serialize()
    {
        UpdatedAt = DateTime.UtcNow;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Name);
        writer.Write(CreatedAt.Ticks);
        writer.Write(UpdatedAt.Ticks);
        writer.Write(Metadata.Count);

        foreach (var kvp in Metadata)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        return ms.ToArray();
    }

    public void Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        Name = reader.ReadString();
        var createdAtTicks = reader.ReadInt64();
        var updatedAtTicks = reader.ReadInt64();
        var count = reader.ReadInt32();

        Metadata.Clear();

        for (var i = 0; i < count; i++)
        {
            Metadata[reader.ReadString()] = reader.ReadString();
        }
    }

    #endregion
}
