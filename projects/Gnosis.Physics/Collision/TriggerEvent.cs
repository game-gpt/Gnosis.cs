using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Collision;

public sealed class TriggerEvent : ITriggerEvent
{
    #region 属性

    public ICollider ThisCollider { get; }

    public ICollider OtherCollider { get; }

    #endregion

    #region 构造函数

    public TriggerEvent(ICollider thisCollider, ICollider otherCollider)
    {
        ThisCollider = thisCollider;
        OtherCollider = otherCollider;
    }

    #endregion
}
