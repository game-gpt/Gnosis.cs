namespace Gnosis.Animation.Blend;

public sealed class BlendNode : IBlendNode
{
    #region 属性

    public string Name { get; }

    public string ClipName { get; }

    public float[] Position { get; set; } = [0f, 0f];

    public float Speed { get; set; } = 1.0f;

    #endregion

    #region 构造函数

    public BlendNode(string name, string clipName)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ClipName = clipName ?? throw new ArgumentNullException(nameof(clipName));
    }

    #endregion
}
