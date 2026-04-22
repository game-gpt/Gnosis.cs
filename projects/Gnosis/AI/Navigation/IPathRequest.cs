namespace Gnosis.AI.Navigation;

public interface IPathRequest
{
    float[] Start { get; }
    float[] End { get; }
    int AreaMask { get; }
    float CostMultiplier { get; }
}
