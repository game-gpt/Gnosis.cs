namespace Gnosis.Widget.Style;

public sealed class StyleDeclaration
{
    public string Property { get; }

    public string Value { get; }

    public string? Important { get; }

    public int Specificity { get; }

    public StyleDeclaration(string property, string value, int specificity = 0, string? important = null)
    {
        Property = property;
        Value = value;
        Specificity = specificity;
        Important = important;
    }
}
