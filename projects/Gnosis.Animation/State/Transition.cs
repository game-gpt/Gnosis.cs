namespace Gnosis.Animation.State;

public sealed class Transition : ITransition
{
    #region 属性

    public string SourceState { get; }

    public string DestinationState { get; }

    public float Duration { get; set; } = 0.25f;

    public float ExitTime { get; set; }

    public bool HasExitTime { get; set; }

    public string Condition { get; set; } = "";

    #endregion

    #region 构造函数

    public Transition(string sourceState, string destinationState)
    {
        SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
        DestinationState = destinationState ?? throw new ArgumentNullException(nameof(destinationState));
    }

    #endregion
}
