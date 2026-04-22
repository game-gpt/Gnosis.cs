namespace Gnosis.Widget.Element;

public delegate void WidgetEventHandler<T>(WidgetElement sender, T e) where T : WidgetEventArgs;
