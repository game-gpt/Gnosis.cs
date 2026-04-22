namespace Gnosis.Widget.Style;

public sealed class ScssVariableScope
{
    private readonly Dictionary<string, string> _variables = new();
    private readonly ScssVariableScope? _parent;

    public ScssVariableScope? Parent => _parent;

    public ScssVariableScope(ScssVariableScope? parent = null)
    {
        _parent = parent;
    }

    public void Define(string name, string value)
    {
        _variables[name] = value;
    }

    public bool TryResolve(string name, out string value)
    {
        if (_variables.TryGetValue(name, out value!))
        {
            return true;
        }

        if (_parent != null)
        {
            return _parent.TryResolve(name, out value!);
        }

        value = default!;
        return false;
    }

    public string? Resolve(string name)
    {
        return TryResolve(name, out var value) ? value : null;
    }

    public ScssVariableScope Push()
    {
        return new ScssVariableScope(this);
    }

    public IReadOnlyDictionary<string, string> AllVariables
    {
        get
        {
            var result = new Dictionary<string, string>();
            CollectInto(result);
            return result;
        }
    }

    private void CollectInto(Dictionary<string, string> target)
    {
        _parent?.CollectInto(target);

        foreach (var (key, value) in _variables)
        {
            target[key] = value;
        }
    }
}
