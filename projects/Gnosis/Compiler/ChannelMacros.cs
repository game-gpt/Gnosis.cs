namespace Gnosis.Compiler;

public sealed record ChannelMacros(IReadOnlyDictionary<string, string> Macros)
{
    public ChannelMacros() : this(new Dictionary<string, string>()) { }

    public string? Get(string name) => Macros.TryGetValue(name, out var value) ? value : null;

    public bool Contains(string name) => Macros.ContainsKey(name);
}
