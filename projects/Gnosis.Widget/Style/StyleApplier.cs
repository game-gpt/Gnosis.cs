using Gnosis.Widget.Element;

namespace Gnosis.Widget.Style;

public sealed class StyleApplier
{
    private readonly ThemeManager _themeManager;

    public StyleApplier(ThemeManager themeManager)
    {
        _themeManager = themeManager;
    }

    public void ApplyAll(WidgetElement root)
    {
        ApplyRecursive(root);
    }

    private void ApplyRecursive(WidgetElement element)
    {
        _themeManager.ApplyStyle(element);

        if (element is ContainerElement container)
        {
            foreach (var child in container.Children)
            {
                ApplyRecursive(child);
            }
        }
    }

    public void InvalidateAll(WidgetElement root)
    {
        ApplyRecursive(root);
        root.InvalidateMeasure();
    }
}
