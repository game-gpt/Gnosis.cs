using Gnosis.Storage.Provider;

namespace Gnosis.Storage.Pref;

public sealed class PlayerPreferences
{
    #region 字段

    private readonly Dictionary<string, string> _values = new();
    private readonly IStorageProvider _provider;

    #endregion

    #region 构造函数

    public PlayerPreferences(IStorageProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    #endregion

    #region 公开方法

    public void SetString(string key, string value)
    {
        _values[key] = value;
    }

    public string GetString(string key, string defaultValue = "")
    {
        return _values.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        _values[key] = value.ToString();
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return _values.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    public void SetFloat(string key, float value)
    {
        _values[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return _values.TryGetValue(key, out var value) && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    public bool HasKey(string key)
    {
        return _values.ContainsKey(key);
    }

    public void DeleteKey(string key)
    {
        _values.Remove(key);
    }

    public async Task SaveAsync()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(_values.Count);

        foreach (var kvp in _values)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        await _provider.SaveAsync("player_preferences", ms.ToArray());
    }

    public async Task LoadAsync()
    {
        var data = await _provider.LoadAsync("player_preferences");

        if (data is null)
        {
            return;
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var count = reader.ReadInt32();
        _values.Clear();

        for (var i = 0; i < count; i++)
        {
            _values[reader.ReadString()] = reader.ReadString();
        }
    }

    #endregion
}
