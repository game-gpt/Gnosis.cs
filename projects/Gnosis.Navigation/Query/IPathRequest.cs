using Gnosis.Core.Math;

namespace Gnosis.Navigation.Query;

public interface IPathRequest
{
    Vector3 Start { get; }
    Vector3 End { get; }
    int AreaMask { get; }
    float CostMultiplier { get; }
}
