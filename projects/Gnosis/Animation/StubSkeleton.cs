namespace Gnosis.Animation;

public class StubSkeleton : ISkeleton
{
    public string Name => throw new NotImplementedException("动画系统尚未实现");
    public IReadOnlyList<IBone> Bones => throw new NotImplementedException("动画系统尚未实现");

    public IBone GetBone(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public int GetBoneIndex(string name)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public IBonePose GetBonePose(int boneIndex)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public float[] GetSkinMatrices()
    {
        throw new NotImplementedException("动画系统尚未实现");
    }

    public void SetBonePose(int boneIndex, IBonePose pose)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}
