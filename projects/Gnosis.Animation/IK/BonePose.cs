namespace Gnosis.Animation.IK;

public sealed class BonePose : IBonePose
{
    #region 属性

    public string BoneName { get; }

    public float[] Position { get; set; } = [0f, 0f, 0f];

    public float[] Rotation { get; set; } = [0f, 0f, 0f, 1f];

    public float[] Scale { get; set; } = [1f, 1f, 1f];

    #endregion

    #region 构造函数

    public BonePose(string boneName)
    {
        BoneName = boneName ?? throw new ArgumentNullException(nameof(boneName));
    }

    #endregion
}
