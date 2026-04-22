using Gnosis.Animation.State;

namespace Gnosis.Animation.State;

public sealed class AnimationLayer : IAnimationLayer
{
    #region 属性

    public string Name { get; }

    public float Weight { get; set; } = 1.0f;

    public IAnimationStateMachine StateMachine { get; set; }

    public string MaskName { get; set; } = "";

    public bool IsAdditive { get; set; }

    public bool IsEnabled { get; set; } = true;

    #endregion

    #region 构造函数

    public AnimationLayer(string name, float weight = 1.0f)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Weight = weight;
        StateMachine = new AnimationStateMachine($"{name}_StateMachine");
    }

    #endregion
}
