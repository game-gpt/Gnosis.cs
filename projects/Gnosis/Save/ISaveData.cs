namespace Gnosis.Save;

public interface ISaveData
{
    string Name { get; }
    long Timestamp { get; }
    int Version { get; }
    void Write(string key, object value);
    T? Read<T>(string key);
    bool HasKey(string key);
    void RemoveKey(string key);
    void Clear();
}

public interface ISaveSerializer
{
    string Name { get; }
    byte[] Serialize(ISaveData data);
    ISaveData Deserialize(byte[] bytes);
}
