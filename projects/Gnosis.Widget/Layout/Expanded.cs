using Gnosis.Widget.Element;

namespace Gnosis.Widget.Layout;

public sealed class Expanded : Flexible
{
    public Expanded(WidgetElement child, int flex = 1) : base(child, flex, FlexFit.Tight)
    {
    }
}
