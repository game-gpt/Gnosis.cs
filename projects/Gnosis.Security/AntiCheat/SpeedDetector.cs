using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

public sealed class SpeedDetector
{
    #region 字段

    private readonly Dictionary<PlayerId, PositionRecord> _lastPositions = new();
    private readonly float _maxSpeed;
    private readonly float _toleranceFactor;

    #endregion

    #region 属性

    public int AnomalousPlayerCount => _lastPositions.Count(kvp => kvp.Value.IsAnomalous);

    #endregion

    #region 事件

    public event Action<PlayerId, float, float>? OnSpeedAnomaly;

    #endregion

    #region 构造函数

    public SpeedDetector(float maxSpeed, float toleranceFactor = 1.2f)
    {
        _maxSpeed = maxSpeed;
        _toleranceFactor = toleranceFactor;
    }

    #endregion

    #region 公开方法

    public bool RecordPosition(PlayerId playerId, float x, float y, float z, float timestampSeconds)
    {
        var current = new PositionData(x, y, z, timestampSeconds);

        if (!_lastPositions.TryGetValue(playerId, out var last))
        {
            _lastPositions[playerId] = new PositionRecord(current, false);
            return false;
        }

        var deltaTime = current.Timestamp - last.Position.Timestamp;
        if (deltaTime <= 0) return last.IsAnomalous;

        var dx = current.X - last.Position.X;
        var dy = current.Y - last.Position.Y;
        var dz = current.Z - last.Position.Z;
        var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        var speed = distance / deltaTime;

        var isAnomalous = speed > _maxSpeed * _toleranceFactor;
        _lastPositions[playerId] = new PositionRecord(current, isAnomalous);

        if (isAnomalous) OnSpeedAnomaly?.Invoke(playerId, speed, _maxSpeed);

        return isAnomalous;
    }

    public bool IsAnomalous(PlayerId playerId)
    {
        return _lastPositions.TryGetValue(playerId, out var record) && record.IsAnomalous;
    }

    public void Reset(PlayerId playerId) => _lastPositions.Remove(playerId);

    public void ResetAll() => _lastPositions.Clear();

    #endregion

    private sealed record PositionRecord(PositionData Position, bool IsAnomalous);
    private sealed record PositionData(float X, float Y, float Z, float Timestamp);
}
