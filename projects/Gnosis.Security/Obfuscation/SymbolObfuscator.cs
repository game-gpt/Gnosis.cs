using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class SymbolObfuscator
{
    #region 字段

    private readonly Dictionary<string, string> _originalToObfuscated = new();
    private readonly Dictionary<string, string> _obfuscatedToOriginal = new();
    private int _counter;

    #endregion

    #region 属性

    public int ObfuscatedSymbolCount => _originalToObfuscated.Count;

    #endregion

    #region 公开方法

    public string Obfuscate(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName)) throw new SecurityException("符号名不能为空");

        if (_originalToObfuscated.TryGetValue(symbolName, out var existing)) return existing;

        var obfuscated = GenerateObfuscatedName();
        _originalToObfuscated[symbolName] = obfuscated;
        _obfuscatedToOriginal[obfuscated] = symbolName;

        return obfuscated;
    }

    public string Restore(string obfuscatedName)
    {
        if (!_obfuscatedToOriginal.TryGetValue(obfuscatedName, out var original))
        {
            throw new SecurityException($"未找到混淆符号 '{obfuscatedName}' 的原始名称");
        }

        return original;
    }

    public List<string> ObfuscateRange(IEnumerable<string> symbolNames)
    {
        var result = new List<string>();
        foreach (var name in symbolNames)
        {
            result.Add(Obfuscate(name));
        }
        return result;
    }

    public void Clear()
    {
        _originalToObfuscated.Clear();
        _obfuscatedToOriginal.Clear();
        _counter = 0;
    }

    #endregion

    private string GenerateObfuscatedName()
    {
        _counter++;
        return $"_{Convert.ToBase64String(BitConverter.GetBytes(_counter)).TrimEnd('=').Replace('+', '_').Replace('/', '_')}";
    }
}
