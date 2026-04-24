using Gnosis.Core.Math;

namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群代理实现，基于 RVO（Reciprocal Velocity Obstacles）进行局部避障
/// </summary>
public sealed class CrowdAgent : ICrowdAgent
{
    #region 字段

    private Vector3 _position;
    private Vector3 _target;
    private Vector3 _velocity;
    private Vector3 _desiredVelocity;

    #endregion

    #region 属性

    /// <summary>
    /// 代理 ID
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 当前位置
    /// </summary>
    public Vector3 Position => _position;

    /// <summary>
    /// 目标位置
    /// </summary>
    public Vector3 Target => _target;

    /// <summary>
    /// 当前速度
    /// </summary>
    public Vector3 Velocity => _velocity;

    /// <summary>
    /// 代理参数
    /// </summary>
    public CrowdAgentParams Params { get; set; }

    /// <summary>
    /// 是否已到达目标
    /// </summary>
    public bool HasReachedTarget
    {
        get
        {
            var distance = Vector3.Distance(_position, _target);
            return distance <= Params.Radius;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建人群代理
    /// </summary>
    /// <param name="id">代理 ID</param>
    /// <param name="position">初始位置</param>
    /// <param name="parameters">代理参数</param>
    public CrowdAgent(int id, Vector3 position, CrowdAgentParams parameters)
    {
        Id = id;
        _position = position;
        _target = position;
        _velocity = Vector3.Zero;
        _desiredVelocity = Vector3.Zero;
        Params = parameters;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 设置目标位置
    /// </summary>
    /// <param name="target">目标位置</param>
    public void SetTarget(Vector3 target)
    {
        _target = target;
    }

    /// <summary>
    /// 重置代理状态
    /// </summary>
    public void Reset()
    {
        _velocity = Vector3.Zero;
        _desiredVelocity = Vector3.Zero;
    }

    /// <summary>
    /// 计算期望速度（朝向目标）
    /// </summary>
    internal void ComputeDesiredVelocity()
    {
        if (HasReachedTarget)
        {
            _desiredVelocity = Vector3.Zero;
            return;
        }

        var dir = _target - _position;
        var length = dir.Length();

        if (length < 0.001f)
        {
            _desiredVelocity = Vector3.Zero;
            return;
        }

        var speed = Math.Min(Params.MaxSpeed, length);
        _desiredVelocity = Vector3.Normalize(dir) * speed;
    }

    /// <summary>
    /// 应用 RVO 避障调整
    /// </summary>
    /// <param name="otherAgents">其他代理列表</param>
    internal void ApplyRVO(IReadOnlyList<ICrowdAgent> otherAgents)
    {
        var avoid = Vector3.Zero;

        foreach (var other in otherAgents)
        {
            if (other.Id == Id)
            {
                continue;
            }

            var diff = _position - other.Position;
            var distSq = diff.LengthSquared();
            var minDist = Params.Radius + other.Params.Radius;
            var minDistSq = minDist * minDist;

            if (distSq < minDistSq && distSq > 0.001f)
            {
                var dist = MathF.Sqrt(distSq);
                var overlap = minDist - dist;
                var weight = Params.SeparationWeight * overlap;
                avoid += Vector3.Normalize(diff) * weight;
            }
        }

        _desiredVelocity += avoid;

        var speed = _desiredVelocity.Length();

        if (speed > Params.MaxSpeed)
        {
            _desiredVelocity = Vector3.Normalize(_desiredVelocity) * Params.MaxSpeed;
        }
    }

    /// <summary>
    /// 更新代理位置
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    internal void Integrate(float delta)
    {
        var accelFactor = Math.Min(1.0f, Params.MaxAcceleration * delta);

        _velocity += (_desiredVelocity - _velocity) * accelFactor;
        _position += _velocity * delta;
    }

    #endregion
}
