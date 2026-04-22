using System.Text;

namespace Gnosis.Toolchain.ScriptCompiler;

public sealed class MacroTable : IMacroTable
{
    #region Fields

    private readonly Dictionary<string, string> _macros = new(StringComparer.Ordinal);

    #endregion

    #region Public Methods

    public void Add(string name, string value)
    {
        _macros[name] = value;
    }

    public string? Get(string name)
    {
        return _macros.GetValueOrDefault(name);
    }

    public bool Contains(string name)
    {
        return _macros.ContainsKey(name);
    }

    public void Remove(string name)
    {
        _macros.Remove(name);
    }

    public void Clear()
    {
        _macros.Clear();
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        return _macros;
    }

    public string ComputeHash()
    {
        var sb = new StringBuilder();

        foreach (var (key, value) in _macros.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
            sb.Append(';');
        }

        return Cache.CacheKeyGenerator.ComputeHash(sb.ToString());
    }

    #endregion
}
