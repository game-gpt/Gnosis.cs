namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作类型
/// </summary>
public enum XrActionType
{
    /// <summary>
    /// 布尔动作（如按钮按下/松开）
    /// </summary>
    Boolean,

    /// <summary>
    /// 浮点动作（如扳机模拟量 0.0 ~ 1.0）
    /// </summary>
    Float,

    /// <summary>
    /// 二维向量动作（如摇杆 X/Y 轴）
    /// </summary>
    Vector2,

    /// <summary>
    /// 姿态动作（如控制器位置/旋转）
    /// </summary>
    Pose,

    /// <summary>
    /// 触觉输出动作
    /// </summary>
    Vibration
}
