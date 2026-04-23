using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;
using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.Path;

public interface IPathfinder
{
    IPath FindPath(Vector3 start, Vector3 end);
    IPath FindPath(IPathRequest request);
    void UpdateNavMesh(INavMesh navMesh);
    void SetAreaCost(int area, float cost);
}
