namespace Gnosis.Editor.GameUI;

public interface IStyleBox
{
    int StyleId { get; }
    float BorderWidth { get; }
    float BorderRadius { get; }
    float PaddingLeft { get; }
    float PaddingRight { get; }
    float PaddingTop { get; }
    float PaddingBottom { get; }
    float BorderR { get; }
    float BorderG { get; }
    float BorderB { get; }
    float BorderA { get; }
    float FillR { get; }
    float FillG { get; }
    float FillB { get; }
    float FillA { get; }
}
