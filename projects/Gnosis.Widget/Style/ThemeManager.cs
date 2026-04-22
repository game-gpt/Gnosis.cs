namespace Gnosis.Widget.Style;

public sealed class ThemeManager
{
    private readonly Dictionary<string, Theme> _themes = new();
    private Theme? _currentTheme;
    private readonly List<StyleSheet> _styleSheets = [];

    public Theme? CurrentTheme => _currentTheme;

    public event Action<Theme>? ThemeChanged;

    public ThemeManager()
    {
        var darkTheme = Theme.CreateDarkTheme();
        var lightTheme = Theme.CreateLightTheme();

        _themes[darkTheme.Name] = darkTheme;
        _themes[lightTheme.Name] = lightTheme;

        _currentTheme = darkTheme;
    }

    public void RegisterTheme(Theme theme)
    {
        _themes[theme.Name] = theme;
    }

    public bool SetTheme(string name)
    {
        if (!_themes.TryGetValue(name, out var theme))
        {
            return false;
        }

        _currentTheme = theme;
        ThemeChanged?.Invoke(theme);
        return true;
    }

    public Theme? GetTheme(string name)
    {
        return _themes.GetValueOrDefault(name);
    }

    public IReadOnlyList<string> AvailableThemes => _themes.Keys.ToList();

    public void AddStyleSheet(StyleSheet sheet)
    {
        _styleSheets.Add(sheet);
    }

    public void RemoveStyleSheet(StyleSheet sheet)
    {
        _styleSheets.Remove(sheet);
    }

    public IReadOnlyList<StyleSheet> StyleSheets => _styleSheets;

    public StyleMatchResult ResolveStyle(Element.WidgetElement element)
    {
        var result = new StyleMatchResult();

        foreach (var sheet in _styleSheets)
        {
            var match = sheet.Match(element);
            foreach (var decl in match.Declarations)
            {
                var resolvedValue = _currentTheme?.ResolveValue(decl.Value) ?? decl.Value;
                result.Add(new StyleDeclaration(decl.Property, resolvedValue, decl.Specificity, decl.Important));
            }
        }

        return result;
    }

    public void ApplyStyle(Element.WidgetElement element)
    {
        var result = ResolveStyle(element);
        ApplyDeclarations(element, result);
    }

    private void ApplyDeclarations(Element.WidgetElement element, StyleMatchResult result)
    {
        foreach (var decl in result.Declarations)
        {
            ApplyDeclaration(element, decl);
        }
    }

    private void ApplyDeclaration(Element.WidgetElement element, StyleDeclaration decl)
    {
        switch (decl.Property.ToLowerInvariant())
        {
            case "background":
            case "background-color":
                ApplyColor(element, decl.Value, static (e, c) => e.Background = c);
                break;
            case "foreground":
            case "color":
                ApplyColor(element, decl.Value, static (e, c) => e.Foreground = c);
                break;
            case "border-color":
                ApplyColor(element, decl.Value, static (e, c) => e.BorderColor = c);
                break;
            case "margin":
                ApplyEdgeInsets(element, decl.Value, static (e, v) => e.Margin = v);
                break;
            case "padding":
                ApplyEdgeInsets(element, decl.Value, static (e, v) => e.Padding = v);
                break;
            case "border":
                ApplyEdgeInsets(element, decl.Value, static (e, v) => e.Border = v);
                break;
            case "width":
                if (float.TryParse(decl.Value, out var w))
                {
                    element.Width = w;
                }

                break;
            case "height":
                if (float.TryParse(decl.Value, out var h))
                {
                    element.Height = h;
                }

                break;
            case "min-width":
                if (float.TryParse(decl.Value, out var minW))
                {
                    element.MinWidth = minW;
                }

                break;
            case "min-height":
                if (float.TryParse(decl.Value, out var minH))
                {
                    element.MinHeight = minH;
                }

                break;
            case "max-width":
                if (float.TryParse(decl.Value, out var maxW))
                {
                    element.MaxWidth = maxW;
                }

                break;
            case "max-height":
                if (float.TryParse(decl.Value, out var maxH))
                {
                    element.MaxHeight = maxH;
                }

                break;
            case "visibility":
                element.Visibility = decl.Value.ToLowerInvariant() switch
                {
                    "visible" => Element.Visibility.Visible,
                    "hidden" => Element.Visibility.Hidden,
                    "collapsed" => Element.Visibility.Collapsed,
                    _ => element.Visibility
                };
                break;
        }
    }

    private void ApplyColor(Element.WidgetElement element, string value, Action<Element.WidgetElement, Element.Color> setter)
    {
        var styleColor = _currentTheme?.ResolveColor(value);

        if (styleColor != null)
        {
            setter(element, styleColor.Value.ToWidgetColor());
        }
        else if (value.StartsWith("#"))
        {
            var c = StyleColor.FromHex(value);
            setter(element, c.ToWidgetColor());
        }
    }

    private void ApplyEdgeInsets(Element.WidgetElement element, string value, Action<Element.WidgetElement, Element.EdgeInsets> setter)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var values = new float[4];

        for (var i = 0; i < parts.Length && i < 4; i++)
        {
            if (!float.TryParse(parts[i], out values[i]))
            {
                return;
            }
        }

        var edgeInsets = parts.Length switch
        {
            1 => new Element.EdgeInsets(values[0]),
            2 => new Element.EdgeInsets(values[0], values[1]),
            4 => new Element.EdgeInsets(values[0], values[1], values[2], values[3]),
            _ => Element.EdgeInsets.Zero
        };

        setter(element, edgeInsets);
    }
}
