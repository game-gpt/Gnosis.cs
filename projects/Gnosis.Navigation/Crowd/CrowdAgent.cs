namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群代理实现，基于 RVO（Reciprocal Velocity Obstacles）进行局部避障
/// </summary>
public sealed class CrowdAgent : ICrowdAgent
{
    #region 字段

    private float[] _position;
    private float[] _target;
    private float[] _velocity;
    private float[] _desiredVelocity;

    #endregion

    #region 属性

    /// <summary>
    /// 代理 ID
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 当前位置
    /// </summary>
    public float[] Position => _position;

    /// <summary>
    /// 目标位置
    /// </summary>
    public float[] Target => _target;

    /// <summary>
    /// 当前速度
    /// </summary>
    public float[] Velocity => _velocity;

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
            float distance = ComputeDistance(_position, _target);
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
    public CrowdAgent(int id, float[] position, CrowdAgentParams parameters)
    {
        Id = id;
        _position = [.. position];
        _target = [.. position];
        _velocity = [0f, 0f, 0f];
        _desiredVelocity = [0f, 0f, 0f];
        Params = parameters;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 设置目标位置
    /// </summary>
    /// <param name="target">目标位置</param>
    public void SetTarget(float[] target)
    {
        _target = [.. target];
    }

    /// <summary>
    /// 重置代理状态
    /// </summary>
    public void Reset()
    {
        _velocity = [0f, 0f, 0f];
        _desiredVelocity = [0f, 0f, 0f];
    }

    /// <summary>
    /// 计算期望速度（朝向目标）
    /// </summary>
    internal void ComputeDesiredVelocity()
    {
        if (HasReachedTarget)
        {
            _desiredVelocity = [0f, 0f, 0f];
            return;
        }

        float dx = _target[0] - _position[0];
        float dy = _target[1] - _position[1];
        float dz = _target[2] - _position[2];

        float length = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        if (length < 0.001f)
        {
            _desiredVelocity = [0f, 0f, 0f];
            return;
        }

        float invLength = 1.0f / length;
        float speed = Math.Min(Params.MaxSpeed, length);

        _desiredVelocity = [dx * invLength * speed, dy * invLength * speed, dz * invLength * speed];
    }

    /// <summary>
    /// 应用 RVO 避障调整
    /// </summary>
    /// <param name="otherAgents">其他代理列表</param>
    internal void ApplyRVO(IReadOnlyList<ICrowdAgent> otherAgents)
    {
        float avoidX = 0f;
        float avoidY = 0f;
        float avoidZ = 0f;

        foreach (var other in otherAgents)
        {
            if (other.Id == Id)
            {
                continue;
            }

            float dx = _position[0] - other.Position[0];
            float dy = _position[1] - other.Position[1];
            float dz = _position[2] - other.Position[2];

            float distSq = dx * dx + dy * dy + dz * dz;
            float minDist = Params.Radius + other.Params.Radius;
            float minDistSq = minDist * minDist;

            if (distSq < minDistSq && distSq > 0.001f)
            {
                float dist = MathF.Sqrt(distSq);
                float overlap = minDist - dist;
                float invDist = 1.0f / dist;

                float weight = Params.SeparationWeight * overlap;
                avoidX += dx * invDist * weight;
                avoidY += dy * invDist * weight;
                avoidZ += dz * invDist * weight;
            }
        }

        _desiredVelocity[0] += avoidX;
        _desiredVelocity[1] += avoidY;
        _desiredVelocity[2] += avoidZ;

        float speed = MathF.Sqrt(
            _desiredVelocity[0] * _desiredVelocity[0] +
            _desiredVelocity[1] * _desiredVelocity[1] +
            _desiredVelocity[2] * _desiredVelocity[2]);

        if (speed > Params.MaxSpeed)
        {
            float scale = Params.MaxSpeed / speed;
            _desiredVelocity[0] *= scale;
            _desiredVelocity[1] *= scale;
            _desiredVelocity[2] *= scale;
        }
    }

    /// <summary>
    /// 更新代理位置
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    internal void Integrate(float delta)
    {
        float accelFactor = Math.Min(1.0f, Params.MaxAcceleration * delta);

        _velocity[0] += (_desiredVelocity[0] - _velocity[0]) * accelFactor;
        _velocity[1] += (_desiredVelocity[1] - _velocity[1]) * accelFactor;
        _velocity[2] += (_desiredVelocity[2] - _velocity[2]) * accelFactor;

        _position[0] += _velocity[0] * delta;
        _position[1] += _velocity[1] * delta;
        _position[2] += _velocity[2] * delta;
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
