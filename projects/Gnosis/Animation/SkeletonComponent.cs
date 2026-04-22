using Gnosis.Animation;
using Gnosis.ECS.Core;

namespace Gnosis.Animation;

public struct SkeletonComponent : IComponent
{
    public ISkeleton? Skeleton { get; set; }
    public string SkeletonName { get; set; }
}
