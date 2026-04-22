namespace Gnosis.AI.State;

/// <summary>
/// 目标选择器实现，管理 AI 的当前目标位置和距离计算
/// </summary>
public sealed class TargetSelector : ITargetSelector
{
    #region 字段

    private float[] _ownerPosition = [0, 0, 0];
    private float[]? _currentTargetPosition;

    #endregion

    #region 属性

    /// <summary>
    /// 当前目标（与 CurrentTargetPosition 相同）
    /// </summary>
    public float[]? CurrentTarget => _currentTargetPosition;

    /// <summary>
    /// 当前目标位置
    /// </summary>
    public float[]? CurrentTargetPosition => _currentTargetPosition;

    /// <summary>
    /// 是否有目标
    /// </summary>
    public bool HasTarget => _currentTargetPosition is not null;

    /// <summary>
    /// 与目标的距离
    /// </summary>
    public float TargetDistance
    {
        get
        {
            if (_currentTargetPosition is null)
            {
                return 0f;
            }

            return ComputeDistance(_ownerPosition, _currentTargetPosition);
        }
    }

    /// <summary>
    /// 感知者位置（需每帧同步）
    /// </summary>
    public float[] OwnerPosition
    {
        get => _ownerPosition;
        set => _ownerPosition = value;
    }

    /// <summary>
    /// 目标丢失距离阈值，超过此距离自动清除目标
    /// </summary>
    public float LoseTargetDistance { get; set; } = float.MaxValue;

    #endregion

    #region 公有方法

    /// <summary>
    /// 设置目标位置
    /// </summary>
    /// <param name="position">目标位置</param>
    public void SetTarget(float[] position)
    {
        _currentTargetPosition = new float[position.Length];
        Array.Copy(position, _currentTargetPosition, position.Length);
    }

    /// <summary>
    /// 清除目标
    /// </summary>
    public void ClearTarget()
    {
        _currentTargetPosition = null;
    }

    /// <summary>
    /// 更新目标选择器，检查目标是否超出丢失距离
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        if (_currentTargetPosition is not null && LoseTargetDistance < float.MaxValue)
        {
            float distance = ComputeDistance(_ownerPosition, _currentTargetPosition);

            if (distance > LoseTargetDistance)
            {
                ClearTarget();
            }
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 计算两点间距离
    /// </summary>
    private static float ComputeDistance(float[] a, float[] b)
    {
        if (a.Length < 3 || b.Length < 3)
        {
            return 0f;
        }

        float dx = a[0] - b[0];
        float dy = a[1] - b[1];
        float dz = a[2] - b[2];

        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    #endregion
}
