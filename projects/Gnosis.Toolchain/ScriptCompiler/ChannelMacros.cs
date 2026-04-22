namespace Gnosis.Toolchain.ScriptCompiler;

public sealed record ChannelMacros(IReadOnlyDictionary<string, string> Macros)
{
    public ChannelMacros() : this(new Dictionary<string, string>()) { }

    public string? Get(string name) => Macros.GetValueOrDefault(name);

    public bool Contains(string name) => Macros.ContainsKey(name);
}
