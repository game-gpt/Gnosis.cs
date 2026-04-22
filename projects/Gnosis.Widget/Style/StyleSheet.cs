using Gnosis.Widget.Element;

namespace Gnosis.Widget.Style;

public sealed class StyleSheet
{
    private readonly List<StyleRule> _rules = [];

    public IReadOnlyList<StyleRule> Rules => _rules;

    public string? Source { get; }

    public StyleSheet(string? source = null)
    {
        Source = source;
    }

    public void AddRule(StyleRule rule)
    {
        _rules.Add(rule);
    }

    public void AddRule(IReadOnlyList<StyleSelector> selectors, IReadOnlyList<StyleDeclaration> declarations)
    {
        _rules.Add(new StyleRule(selectors, declarations));
    }

    public void Clear()
    {
        _rules.Clear();
    }

    public StyleMatchResult Match(WidgetElement element)
    {
        var result = new StyleMatchResult();
        var candidates = new List<(StyleRule Rule, StyleDeclaration Decl)>();

        foreach (var rule in _rules)
        {
            if (!rule.Matches(element))
            {
                continue;
            }

            foreach (var decl in rule.Declarations)
            {
                candidates.Add((rule, decl));
            }
        }

        candidates.Sort((a, b) =>
        {
            var specCmp = a.Rule.Specificity.CompareTo(b.Rule.Specificity);
            if (specCmp != 0)
            {
                return specCmp;
            }

            var importantA = a.Decl.Important != null ? 1 : 0;
            var importantB = b.Decl.Important != null ? 1 : 0;
            return importantA.CompareTo(importantB);
        });

        var seen = new HashSet<string>();

        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var decl = candidates[i].Decl;
            if (seen.Add(decl.Property))
            {
                result.Add(decl);
            }
        }

        return result;
    }

    public static StyleSheet Parse(string source)
    {
        var sheet = new StyleSheet(source);
        var parser = new ScssParser(source);
        parser.Parse(sheet);
        return sheet;
    }

    public static StyleSheet ParseScss(string source)
    {
        return Parse(source);
    }
}
