using Gnosis.GameUI;

namespace Gnosis.GameUI;

public sealed class StyleBox : IStyleBox
{
    #region Properties

    public int StyleId { get; set; }

    public float BorderWidth { get; set; } = 0;

    public float BorderRadius { get; set; } = 0;

    public float PaddingLeft { get; set; } = 0;

    public float PaddingRight { get; set; } = 0;

    public float PaddingTop { get; set; } = 0;

    public float PaddingBottom { get; set; } = 0;

    public float BorderR { get; set; } = 0;

    public float BorderG { get; set; } = 0;

    public float BorderB { get; set; } = 0;

    public float BorderA { get; set; } = 1.0f;

    public float FillR { get; set; } = 0.2f;

    public float FillG { get; set; } = 0.2f;

    public float FillB { get; set; } = 0.2f;

    public float FillA { get; set; } = 1.0f;

    #endregion
}
