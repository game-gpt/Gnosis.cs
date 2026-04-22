namespace Gnosis.Widget.Element;

public sealed class FocusManager
{
    private WidgetElement? _focusedElement;
    private readonly WidgetElement _root;
    private readonly EventRouter _eventRouter = new();

    public WidgetElement? FocusedElement => _focusedElement;

    public FocusManager(WidgetElement root)
    {
        _root = root;
    }

    public bool SetFocus(WidgetElement? element)
    {
        if (element != null && (!element.IsFocusable || !element.IsEnabled))
        {
            return false;
        }

        if (_focusedElement == element)
        {
            return true;
        }

        var oldFocus = _focusedElement;
        var newFocus = element;

        if (oldFocus != null)
        {
            oldFocus.IsFocused = false;
            var lostArgs = new FocusEventArgs(oldFocus, newFocus);
            _eventRouter.RouteDirect(oldFocus, lostArgs);
        }

        _focusedElement = newFocus;

        if (newFocus != null)
        {
            newFocus.IsFocused = true;
            var gotArgs = new FocusEventArgs(oldFocus, newFocus);
            _eventRouter.RouteDirect(newFocus, gotArgs);
        }

        return true;
    }

    public bool MoveNext()
    {
        var focusableElements = new List<WidgetElement>();
        CollectFocusable(_root, focusableElements);

        if (focusableElements.Count == 0)
        {
            return false;
        }

        if (_focusedElement == null)
        {
            return SetFocus(focusableElements[0]);
        }

        var currentIndex = focusableElements.IndexOf(_focusedElement);
        var nextIndex = (currentIndex + 1) % focusableElements.Count;

        return SetFocus(focusableElements[nextIndex]);
    }

    public bool MovePrevious()
    {
        var focusableElements = new List<WidgetElement>();
        CollectFocusable(_root, focusableElements);

        if (focusableElements.Count == 0)
        {
            return false;
        }

        if (_focusedElement == null)
        {
            return SetFocus(focusableElements[^1]);
        }

        var currentIndex = focusableElements.IndexOf(_focusedElement);
        var prevIndex = currentIndex <= 0 ? focusableElements.Count - 1 : currentIndex - 1;

        return SetFocus(focusableElements[prevIndex]);
    }

    private static void CollectFocusable(WidgetElement element, List<WidgetElement> result)
    {
        if (element.IsFocusable && element.IsEnabled && element.Visibility == Visibility.Visible)
        {
            result.Add(element);
        }

        if (element is ContainerElement container)
        {
            foreach (var child in container.Children)
            {
                CollectFocusable(child, result);
            }
        }
    }
}
