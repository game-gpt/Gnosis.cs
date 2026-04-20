namespace GnosisEngine.UI;

// 展开组件，强制子组件填满 Flex 容器中分配的所有可用空间
public sealed class Expanded : Flexible
{
    public Expanded(Widget child, int flex = 1) : base(child, flex, FlexFit.Tight)
    {
    }
}
