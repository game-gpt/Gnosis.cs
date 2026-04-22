namespace Gnosis.Widget.Style;

public sealed class Theme
{
    private readonly Dictionary<string, string> _variables = new();
    private readonly Dictionary<string, StyleColor> _colorVariables = new();

    public string Name { get; }

    public Theme? BaseTheme { get; }

    public IReadOnlyDictionary<string, string> Variables => _variables;

    public Theme(string name, Theme? baseTheme = null)
    {
        Name = name;
        BaseTheme = baseTheme;
    }

    public void SetVariable(string name, string value)
    {
        _variables[NormalizeVarName(name)] = value;
    }

    public void SetColor(string name, StyleColor color)
    {
        _colorVariables[NormalizeVarName(name)] = color;
    }

    public string? GetVariable(string name)
    {
        var key = NormalizeVarName(name);

        if (_variables.TryGetValue(key, out var value))
        {
            return value;
        }

        return BaseTheme?.GetVariable(key);
    }

    public StyleColor? GetColor(string name)
    {
        var key = NormalizeVarName(name);

        if (_colorVariables.TryGetValue(key, out var color))
        {
            return color;
        }

        return BaseTheme?.GetColor(key);
    }

    public string ResolveValue(string value)
    {
        if (value.StartsWith("var(") && value.EndsWith(")"))
        {
            var varName = value.Substring(4, value.Length - 5).Trim();
            var resolved = GetVariable(varName);
            if (resolved != null)
            {
                return ResolveValue(resolved);
            }

            return value;
        }

        if (value.StartsWith("$"))
        {
            var varName = value.Substring(1);
            var resolved = GetVariable(varName);
            if (resolved != null)
            {
                return ResolveValue(resolved);
            }

            return value;
        }

        return value;
    }

    public StyleColor? ResolveColor(string value)
    {
        var resolved = ResolveValue(value);

        if (resolved.StartsWith("#"))
        {
            return StyleColor.FromHex(resolved);
        }

        if (resolved.StartsWith("var("))
        {
            var varName = resolved.Substring(4, resolved.Length - 5).Trim();
            return GetColor(varName);
        }

        if (resolved.StartsWith("$"))
        {
            var varName = resolved.Substring(1);
            return GetColor(varName);
        }

        return NamedColor(resolved);
    }

    private static string NormalizeVarName(string name)
    {
        if (name.StartsWith("$"))
        {
            return name.Substring(1);
        }

        if (name.StartsWith("--"))
        {
            return name;
        }

        return name;
    }

    private StyleColor? NamedColor(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "transparent" => StyleColor.Transparent,
            "white" => StyleColor.White,
            "black" => StyleColor.Black,
            "red" => StyleColor.FromRgb(255, 0, 0),
            "green" => StyleColor.FromRgb(0, 128, 0),
            "blue" => StyleColor.FromRgb(0, 0, 255),
            "gray" or "grey" => StyleColor.FromRgb(128, 128, 128),
            "darkgray" or "darkgrey" => StyleColor.FromRgb(64, 64, 64),
            "lightgray" or "lightgrey" => StyleColor.FromRgb(192, 192, 192),
            _ => null
        };
    }

    public static Theme CreateDarkTheme()
    {
        var theme = new Theme("dark");

        theme.SetColor("--surface", new StyleColor(0.12f, 0.12f, 0.14f));
        theme.SetColor("--surface-variant", new StyleColor(0.18f, 0.18f, 0.22f));
        theme.SetColor("--background", new StyleColor(0.10f, 0.10f, 0.12f));
        theme.SetColor("--border", new StyleColor(0.30f, 0.30f, 0.34f));
        theme.SetColor("--text", new StyleColor(0.90f, 0.90f, 0.92f));
        theme.SetColor("--text-secondary", new StyleColor(0.60f, 0.60f, 0.64f));
        theme.SetColor("--primary", new StyleColor(0.30f, 0.55f, 0.90f));
        theme.SetColor("--primary-hover", new StyleColor(0.35f, 0.60f, 0.95f));
        theme.SetColor("--accent", new StyleColor(0.40f, 0.70f, 0.45f));
        theme.SetColor("--error", new StyleColor(0.85f, 0.25f, 0.25f));
        theme.SetColor("--warning", new StyleColor(0.90f, 0.70f, 0.20f));
        theme.SetColor("--success", new StyleColor(0.30f, 0.75f, 0.40f));
        theme.SetColor("--hover-overlay", new StyleColor(1.0f, 1.0f, 1.0f, 0.06f));
        theme.SetColor("--pressed-overlay", new StyleColor(1.0f, 1.0f, 1.0f, 0.12f));
        theme.SetColor("--selection", new StyleColor(0.30f, 0.55f, 0.90f, 0.30f));

        theme.SetColor("surface", new StyleColor(0.12f, 0.12f, 0.14f));
        theme.SetColor("surface-variant", new StyleColor(0.18f, 0.18f, 0.22f));
        theme.SetColor("background", new StyleColor(0.10f, 0.10f, 0.12f));
        theme.SetColor("border", new StyleColor(0.30f, 0.30f, 0.34f));
        theme.SetColor("text", new StyleColor(0.90f, 0.90f, 0.92f));
        theme.SetColor("text-secondary", new StyleColor(0.60f, 0.60f, 0.64f));
        theme.SetColor("primary", new StyleColor(0.30f, 0.55f, 0.90f));
        theme.SetColor("primary-hover", new StyleColor(0.35f, 0.60f, 0.95f));
        theme.SetColor("accent", new StyleColor(0.40f, 0.70f, 0.45f));
        theme.SetColor("error", new StyleColor(0.85f, 0.25f, 0.25f));
        theme.SetColor("warning", new StyleColor(0.90f, 0.70f, 0.20f));
        theme.SetColor("success", new StyleColor(0.30f, 0.75f, 0.40f));
        theme.SetColor("hover-overlay", new StyleColor(1.0f, 1.0f, 1.0f, 0.06f));
        theme.SetColor("pressed-overlay", new StyleColor(1.0f, 1.0f, 1.0f, 0.12f));
        theme.SetColor("selection", new StyleColor(0.30f, 0.55f, 0.90f, 0.30f));

        theme.SetVariable("--spacing-xs", "2");
        theme.SetVariable("--spacing-sm", "4");
        theme.SetVariable("--spacing-md", "8");
        theme.SetVariable("--spacing-lg", "12");
        theme.SetVariable("--spacing-xl", "16");
        theme.SetVariable("--spacing-xxl", "24");
        theme.SetVariable("--border-width", "1");
        theme.SetVariable("--border-radius", "4");
        theme.SetVariable("--font-size-xs", "10");
        theme.SetVariable("--font-size-sm", "12");
        theme.SetVariable("--font-size-md", "14");
        theme.SetVariable("--font-size-lg", "16");
        theme.SetVariable("--font-size-xl", "20");
        theme.SetVariable("--font-size-xxl", "24");

        theme.SetVariable("spacing-xs", "2");
        theme.SetVariable("spacing-sm", "4");
        theme.SetVariable("spacing-md", "8");
        theme.SetVariable("spacing-lg", "12");
        theme.SetVariable("spacing-xl", "16");
        theme.SetVariable("spacing-xxl", "24");
        theme.SetVariable("border-width", "1");
        theme.SetVariable("border-radius", "4");
        theme.SetVariable("font-size-xs", "10");
        theme.SetVariable("font-size-sm", "12");
        theme.SetVariable("font-size-md", "14");
        theme.SetVariable("font-size-lg", "16");
        theme.SetVariable("font-size-xl", "20");
        theme.SetVariable("font-size-xxl", "24");

        return theme;
    }

    public static Theme CreateLightTheme()
    {
        var theme = new Theme("light");

        theme.SetColor("--surface", new StyleColor(1.0f, 1.0f, 1.0f));
        theme.SetColor("--surface-variant", new StyleColor(0.96f, 0.96f, 0.98f));
        theme.SetColor("--background", new StyleColor(0.94f, 0.94f, 0.96f));
        theme.SetColor("--border", new StyleColor(0.76f, 0.76f, 0.80f));
        theme.SetColor("--text", new StyleColor(0.13f, 0.13f, 0.15f));
        theme.SetColor("--text-secondary", new StyleColor(0.45f, 0.45f, 0.50f));
        theme.SetColor("--primary", new StyleColor(0.15f, 0.40f, 0.85f));
        theme.SetColor("--primary-hover", new StyleColor(0.12f, 0.35f, 0.78f));
        theme.SetColor("--accent", new StyleColor(0.20f, 0.60f, 0.30f));
        theme.SetColor("--error", new StyleColor(0.80f, 0.15f, 0.15f));
        theme.SetColor("--warning", new StyleColor(0.85f, 0.60f, 0.10f));
        theme.SetColor("--success", new StyleColor(0.20f, 0.65f, 0.30f));
        theme.SetColor("--hover-overlay", new StyleColor(0, 0, 0, 0.04f));
        theme.SetColor("--pressed-overlay", new StyleColor(0, 0, 0, 0.08f));
        theme.SetColor("--selection", new StyleColor(0.15f, 0.40f, 0.85f, 0.20f));

        theme.SetColor("surface", new StyleColor(1.0f, 1.0f, 1.0f));
        theme.SetColor("surface-variant", new StyleColor(0.96f, 0.96f, 0.98f));
        theme.SetColor("background", new StyleColor(0.94f, 0.94f, 0.96f));
        theme.SetColor("border", new StyleColor(0.76f, 0.76f, 0.80f));
        theme.SetColor("text", new StyleColor(0.13f, 0.13f, 0.15f));
        theme.SetColor("text-secondary", new StyleColor(0.45f, 0.45f, 0.50f));
        theme.SetColor("primary", new StyleColor(0.15f, 0.40f, 0.85f));
        theme.SetColor("primary-hover", new StyleColor(0.12f, 0.35f, 0.78f));
        theme.SetColor("accent", new StyleColor(0.20f, 0.60f, 0.30f));
        theme.SetColor("error", new StyleColor(0.80f, 0.15f, 0.15f));
        theme.SetColor("warning", new StyleColor(0.85f, 0.60f, 0.10f));
        theme.SetColor("success", new StyleColor(0.20f, 0.65f, 0.30f));
        theme.SetColor("hover-overlay", new StyleColor(0, 0, 0, 0.04f));
        theme.SetColor("pressed-overlay", new StyleColor(0, 0, 0, 0.08f));
        theme.SetColor("selection", new StyleColor(0.15f, 0.40f, 0.85f, 0.20f));

        theme.SetVariable("--spacing-xs", "2");
        theme.SetVariable("--spacing-sm", "4");
        theme.SetVariable("--spacing-md", "8");
        theme.SetVariable("--spacing-lg", "12");
        theme.SetVariable("--spacing-xl", "16");
        theme.SetVariable("--spacing-xxl", "24");
        theme.SetVariable("--border-width", "1");
        theme.SetVariable("--border-radius", "4");
        theme.SetVariable("--font-size-xs", "10");
        theme.SetVariable("--font-size-sm", "12");
        theme.SetVariable("--font-size-md", "14");
        theme.SetVariable("--font-size-lg", "16");
        theme.SetVariable("--font-size-xl", "20");
        theme.SetVariable("--font-size-xxl", "24");

        theme.SetVariable("spacing-xs", "2");
        theme.SetVariable("spacing-sm", "4");
        theme.SetVariable("spacing-md", "8");
        theme.SetVariable("spacing-lg", "12");
        theme.SetVariable("spacing-xl", "16");
        theme.SetVariable("spacing-xxl", "24");
        theme.SetVariable("border-width", "1");
        theme.SetVariable("border-radius", "4");
        theme.SetVariable("font-size-xs", "10");
        theme.SetVariable("font-size-sm", "12");
        theme.SetVariable("font-size-md", "14");
        theme.SetVariable("font-size-lg", "16");
        theme.SetVariable("font-size-xl", "20");
        theme.SetVariable("font-size-xxl", "24");

        return theme;
    }
}
