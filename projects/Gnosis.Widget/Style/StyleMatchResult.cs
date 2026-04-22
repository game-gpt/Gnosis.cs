namespace Gnosis.Widget.Style;

public sealed class StyleMatchResult
{
    private readonly List<StyleDeclaration> _declarations = [];

    public IReadOnlyList<StyleDeclaration> Declarations => _declarations;

    internal void Add(StyleDeclaration declaration)
    {
        _declarations.Add(declaration);
    }

    public string? GetValue(string property)
    {
        for (var i = _declarations.Count - 1; i >= 0; i--)
        {
            if (_declarations[i].Property == property)
            {
                return _declarations[i].Value;
            }
        }

        return null;
    }

    public bool TryGetValue(string property, out string? value)
    {
        value = GetValue(property);
        return value != null;
    }
}
