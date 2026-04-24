namespace Gnosis.AI.State;

/// <summary>
/// 目标选择器实现，管理 AI 的当前目标位置和距离计算
/// </summary>
public sealed class TargetSelector : ITargetSelector
{
    #region 字段

    private Vector3 _ownerPosition;
    private Vector3? _currentTargetPosition;

    #endregion

    #region 属性

    /// <summary>
    /// 当前目标（与 CurrentTargetPosition 相同）
    /// </summary>
    public Vector3? CurrentTarget => _currentTargetPosition;

    /// <summary>
    /// 当前目标位置
    /// </summary>
    public Vector3? CurrentTargetPosition => _currentTargetPosition;

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

            return Vector3.Distance(_ownerPosition, _currentTargetPosition.Value);
        }
    }

    /// <summary>
    /// 感知者位置（需每帧同步）
    /// </summary>
    public Vector3 OwnerPosition
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
    public void SetTarget(Vector3 position)
    {
        _currentTargetPosition = position;
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
            var distance = Vector3.Distance(_ownerPosition, _currentTargetPosition.Value);

            if (distance > LoseTargetDistance)
            {
                ClearTarget();
            }
        }
    }

    #endregion
}
