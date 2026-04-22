namespace Gnosis.Animation.IK;

public struct SkeletonComponent : IComponent
{
    public ISkeleton? Skeleton { get; set; }
    public string SkeletonName { get; set; }
}
