using Gnosis.Core.Math;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作状态，记录动作的当前值与变化信息
/// </summary>
public readonly record struct XrActionState
{
    /// <summary>
    /// 布尔值（按钮动作）
    /// </summary>
    public bool BooleanValue { get; init; }

    /// <summary>
    /// 浮点值（模拟量动作）
    /// </summary>
    public float FloatValue { get; init; }

    /// <summary>
    /// 二维向量值（摇杆/触摸板动作）
    /// </summary>
    public Vector2 Vector2Value { get; init; }

    /// <summary>
    /// 当前帧是否发生变化
    /// </summary>
    public bool ChangedSinceLastFrame { get; init; }

    /// <summary>
    /// 上次变化时间（纳秒）
    /// </summary>
    public long LastChangeTime { get; init; }

    /// <summary>
    /// 动作是否处于激活状态
    /// </summary>
    public bool IsActive { get; init; }
}
