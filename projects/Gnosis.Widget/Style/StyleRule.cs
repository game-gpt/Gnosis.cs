namespace Gnosis.Widget.Style;

public sealed class StyleRule
{
    public IReadOnlyList<StyleSelector> Selectors { get; }

    public IReadOnlyList<StyleDeclaration> Declarations { get; }

    public int Specificity { get; }

    public StyleRule(IReadOnlyList<StyleSelector> selectors, IReadOnlyList<StyleDeclaration> declarations)
    {
        Selectors = selectors;
        Declarations = declarations;
        Specificity = ComputeSpecificity(selectors);
    }

    private static int ComputeSpecificity(IReadOnlyList<StyleSelector> selectors)
    {
        int ids = 0;
        int classes = 0;
        int types = 0;

        foreach (var selector in selectors)
        {
            switch (selector.Type)
            {
                case StyleSelectorType.Id:
                    ids++;
                    break;
                case StyleSelectorType.Class:
                case StyleSelectorType.PseudoClass:
                    classes++;
                    break;
                case StyleSelectorType.Type:
                    types++;
                    break;
                case StyleSelectorType.ParentRef:
                    break;
            }
        }

        return (ids << 16) | (classes << 8) | types;
    }

    public bool Matches(Element.WidgetElement element)
    {
        foreach (var selector in Selectors)
        {
            if (selector.Type == StyleSelectorType.ParentRef)
            {
                continue;
            }

            if (!MatchesSelector(selector, element))
            {
                return false;
            }
        }

        return Selectors.Count > 0;
    }

    private static bool MatchesSelector(StyleSelector selector, Element.WidgetElement element)
    {
        return selector.Type switch
        {
            StyleSelectorType.Type => element.GetType().Name.Equals(selector.Value, StringComparison.OrdinalIgnoreCase),
            StyleSelectorType.Id => element.Id == selector.Value,
            StyleSelectorType.Class => element.StyleClass == selector.Value,
            StyleSelectorType.PseudoClass => MatchesPseudoClass(selector.Value, element),
            _ => false
        };
    }

    private static bool MatchesPseudoClass(string pseudoClass, Element.WidgetElement element)
    {
        return pseudoClass.ToLowerInvariant() switch
        {
            "hover" => element is Render.ButtonWidget btn && btn.IsHovered,
            "pressed" => element is Render.ButtonWidget btn && btn.IsPressed,
            "focus" => element.IsFocused,
            "disabled" => !element.IsEnabled,
            _ => false
        };
    }
}
