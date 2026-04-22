namespace Gnosis.Animation.IK;

public sealed class Skeleton : ISkeleton
{
    #region 字段

    private readonly List<IBone> _bones = new();

    #endregion

    #region 属性

    public string Name { get; }

    public IReadOnlyList<IBone> Bones => _bones;

    #endregion

    #region 构造函数

    public Skeleton(string name, int boneCount)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        for (var i = 0; i < boneCount; i++)
        {
            _bones.Add(new Bone($"Bone_{i}", -1));
        }
    }

    #endregion

    #region ISkeleton 实现

    public IBone? GetBone(string name)
    {
        foreach (var bone in _bones)
        {
            if (bone.Name == name)
            {
                return bone;
            }
        }

        return null;
    }

    public int GetBoneIndex(string name)
    {
        for (var i = 0; i < _bones.Count; i++)
        {
            if (_bones[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    public void SetBonePose(int index, IBonePose pose)
    {
    }

    public IBonePose? GetBonePose(int index)
    {
        return null;
    }

    public float[]? GetSkinMatrices()
    {
        return null;
    }

    #endregion
}
