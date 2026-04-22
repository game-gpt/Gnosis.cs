namespace Gnosis.Animation.IK;

public sealed class Bone : IBone
{
    #region 字段

    private readonly List<IBone> _children = new();

    #endregion

    #region 属性

    public string Name { get; }

    public int ParentIndex { get; }

    public float[] BindPose { get; set; } = [0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f];

    public float[] LocalTransform { get; set; } = [0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f];

    public IBone? Parent { get; set; }

    public IReadOnlyList<IBone> Children => _children;

    #endregion

    #region 构造函数

    public Bone(string name, int parentIndex)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParentIndex = parentIndex;
    }

    #endregion

    #region 内部方法

    internal void AddChild(IBone child)
    {
        _children.Add(child);
    }

    #endregion
}
