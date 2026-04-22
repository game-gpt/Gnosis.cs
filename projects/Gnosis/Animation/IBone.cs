namespace Gnosis.Animation;

public interface IBone
{
    string Name { get; }
    int ParentIndex { get; }
    float[] BindPose { get; }
    float[] LocalTransform { get; set; }
    IBone? Parent { get; }
    IReadOnlyList<IBone> Children { get; }
}
