using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

/// <summary>
/// AI 刺激源实现，表示世界中可被 AI 感知的对象
/// </summary>
public sealed class AIStimulusSource : IAIStimulusSource
{
    #region 字段

    private Vector3 _position;
    private float _strength;

    #endregion

    #region 属性

    /// <summary>
    /// 刺激源唯一标识
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 刺激源位置（3D 坐标）
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    /// <summary>
    /// 刺激强度，0 表示无刺激
    /// </summary>
    public float Strength
    {
        get => _strength;
        set => _strength = Math.Max(0, value);
    }

    /// <summary>
    /// 刺激类型
    /// </summary>
    public AISenseType SenseType { get; }

    /// <summary>
    /// 刺激源是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 刺激源朝向（归一化方向向量），用于视觉感知的视野检测
    /// </summary>
    public Vector3? Forward { get; set; }

    /// <summary>
    /// 刺激源半径，用于触觉感知的碰撞检测
    /// </summary>
    public float Radius { get; set; } = 50.0f;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建刺激源
    /// </summary>
    /// <param name="id">唯一标识</param>
    /// <param name="position">位置</param>
    /// <param name="strength">强度</param>
    /// <param name="senseType">刺激类型</param>
    public AIStimulusSource(int id, Vector3 position, float strength, AISenseType senseType)
    {
        Id = id;
        _position = position;
        _strength = strength;
        SenseType = senseType;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 计算与目标位置的距离
    /// </summary>
    /// <param name="otherPosition">目标位置</param>
    /// <returns>距离</returns>
    public float DistanceTo(Vector3 otherPosition)
    {
        return Vector3.Distance(_position, otherPosition);
    }

    #endregion
}
