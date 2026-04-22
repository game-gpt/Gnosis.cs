namespace Gnosis.Navigation.Query;

public interface IPathRequest
{
    float[] Start { get; }
    float[] End { get; }
    int AreaMask { get; }
    float CostMultiplier { get; }
}
