namespace Gnosis.Animation;

public interface ISkeleton
{
    string Name { get; }
    IReadOnlyList<IBone> Bones { get; }
    IBone GetBone(string name);
    int GetBoneIndex(string name);
    void SetBonePose(int boneIndex, IBonePose pose);
    IBonePose GetBonePose(int boneIndex);
    float[] GetSkinMatrices();
}
