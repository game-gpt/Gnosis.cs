using Gnosis.Widget.Element;

namespace Gnosis.Widget.Style;

public sealed class ThemeManager
{
    #region 字段

    private readonly Dictionary<string, Theme> _themes = new();
    private Theme? _currentTheme;
    private readonly List<StyleSheet> _styleSheets = [];
    private readonly List<WidgetElement> _trackedRoots = [];

    #endregion

    #region 属性

    public Theme? CurrentTheme => _currentTheme;

    public IReadOnlyList<string> AvailableThemes => _themes.Keys.ToList();

    public IReadOnlyList<StyleSheet> StyleSheets => _styleSheets;

    #endregion

    #region 事件

    public event Action<Theme>? ThemeChanged;

    public event Action<Theme, Theme>? ThemeSwitched;

    #endregion

    #region 构造

    public ThemeManager()
    {
        var darkTheme = Theme.CreateDarkTheme();
        var lightTheme = Theme.CreateLightTheme();

        _themes[darkTheme.Name] = darkTheme;
        _themes[lightTheme.Name] = lightTheme;

        _currentTheme = darkTheme;
    }

    #endregion

    #region 主题管理

    public void RegisterTheme(Theme theme)
    {
        _themes[theme.Name] = theme;
    }

    public bool UnregisterTheme(string name)
    {
        if (name == "dark" || name == "light")
        {
            return false;
        }

        if (_currentTheme?.Name == name)
        {
            return false;
        }

        return _themes.Remove(name);
    }

    public bool SetTheme(string name)
    {
        if (!_themes.TryGetValue(name, out var theme))
        {
            return false;
        }

        var oldTheme = _currentTheme;
        _currentTheme = theme;
        ThemeChanged?.Invoke(theme);

        if (oldTheme != null && oldTheme.Name != theme.Name)
        {
            ThemeSwitched?.Invoke(oldTheme, theme);
            RefreshAllTrackedRoots();
        }

        return true;
    }

    public Theme? GetTheme(string name)
    {
        return _themes.GetValueOrDefault(name);
    }

    public void ToggleTheme()
    {
        if (_currentTheme?.Name == "dark")
        {
            SetTheme("light");
        }
        else
        {
            SetTheme("dark");
        }
    }

    #endregion

    #region 根元素追踪

    public void TrackRoot(WidgetElement root, StyleApplier styleApplier)
    {
        if (!_trackedRoots.Contains(root))
        {
            _trackedRoots.Add(root);
        }
    }

    public void UntrackRoot(WidgetElement root)
    {
        _trackedRoots.Remove(root);
    }

    private void RefreshAllTrackedRoots()
    {
        foreach (var root in _trackedRoots)
        {
            ApplyThemeToElement(root);
            root.InvalidateMeasure();
        }
    }

    #endregion

    #region 样式表管理

    public void AddStyleSheet(StyleSheet sheet)
    {
        _styleSheets.Add(sheet);
    }

    public void RemoveStyleSheet(StyleSheet sheet)
    {
        _styleSheets.Remove(sheet);
    }

    #endregion

    #region 样式解析

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

    #endregion

    #region 样式应用

    public void ApplyStyle(Element.WidgetElement element)
    {
        var result = ResolveStyle(element);
        ApplyDeclarations(element, result);
    }

    public void ApplyThemeToElement(WidgetElement element)
    {
        ApplyThemeColorsRecursive(element);
    }

    private void ApplyThemeColorsRecursive(WidgetElement element)
    {
        ApplyThemeColors(element);

        if (element is ContainerElement container)
        {
            foreach (var child in container.Children)
            {
                ApplyThemeColorsRecursive(child);
            }
        }
    }

    private void ApplyThemeColors(WidgetElement element)
    {
        if (_currentTheme == null)
        {
            return;
        }

        var bg = _currentTheme.GetColor("background");
        if (bg != null && element.Background.A > 0)
        {
            element.Background = bg.Value.ToWidgetColor();
        }

        var fg = _currentTheme.GetColor("text");
        if (fg != null && element.Foreground.A > 0)
        {
            element.Foreground = fg.Value.ToWidgetColor();
        }

        var border = _currentTheme.GetColor("border");
        if (border != null && element.BorderColor.A > 0)
        {
            element.BorderColor = border.Value.ToWidgetColor();
        }
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

    #endregion
}
