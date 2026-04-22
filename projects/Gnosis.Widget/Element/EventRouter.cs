namespace Gnosis.Widget.Element;

public sealed class EventRouter
{
    public void RouteBubble<T>(WidgetElement target, T args) where T : WidgetEventArgs
    {
        var current = target;

        while (current != null)
        {
            current.DispatchEvent(args);

            if (args.Handled)
            {
                return;
            }

            current = current.Parent;
        }
    }

    public void RouteDirect<T>(WidgetElement target, T args) where T : WidgetEventArgs
    {
        target.DispatchEvent(args);
    }
}
