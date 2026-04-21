using Gnosis.Core;

namespace Gnosis.Security;

/// <summary>
/// 网络协议蜜罐，在协议层面设置陷阱检测作弊行为
/// </summary>
public sealed class ProtocolHoneypot
{
    #region 字段

    private readonly Dictionary<EntityId, TrapEntity> _trapEntities = new();
    private readonly List<ProtocolTrapField> _trapFields = [];
    private readonly HashSet<PlayerId> _flaggedPlayers = [];

    #endregion

    #region 属性

    /// <summary>
    /// 获取蜜罐实体数量
    /// </summary>
    public int TrapEntityCount => _trapEntities.Count;

    /// <summary>
    /// 获取协议陷阱字段数量
    /// </summary>
    public int TrapFieldCount => _trapFields.Count;

    /// <summary>
    /// 获取被标记的作弊玩家数量
    /// </summary>
    public int FlaggedPlayerCount => _flaggedPlayers.Count;

    #endregion

    #region 公开方法

    /// <summary>
    /// 生成不可达的蜜罐实体，检测玩家是否尝试与其交互
    /// </summary>
    /// <param name="entityId">蜜罐实体 ID</param>
    /// <param name="trapType">陷阱类型</param>
    /// <param name="x">X 坐标（不可达位置）</param>
    /// <param name="y">Y 坐标（不可达位置）</param>
    /// <param name="z">Z 坐标（不可达位置）</param>
    public void SpawnTrapEntity(EntityId entityId, string trapType, float x = 0f, float y = -9999f, float z = 0f)
    {
        _trapEntities[entityId] = new TrapEntity(entityId, trapType, x, y, z);
    }

    /// <summary>
    /// 添加协议陷阱字段，在协议中添加不应被客户端使用的字段
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <param name="fakeValue">诱饵假值</param>
    public void AddTrapField(string fieldName, object fakeValue)
    {
        _trapFields.Add(new ProtocolTrapField(fieldName, fakeValue));
    }

    /// <summary>
    /// 检测玩家与蜜罐实体的交互，如果交互则标记为作弊者
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="targetEntityId">目标实体 ID</param>
    /// <returns>如果是蜜罐实体且玩家尝试交互则返回 true</returns>
    public bool CheckInteraction(PlayerId playerId, EntityId targetEntityId)
    {
        if (_trapEntities.ContainsKey(targetEntityId))
        {
            _flaggedPlayers.Add(playerId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查玩家是否被标记为作弊者
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <returns>如果被标记返回 true</returns>
    public bool IsFlagged(PlayerId playerId)
    {
        return _flaggedPlayers.Contains(playerId);
    }

    /// <summary>
    /// 标记玩家为作弊者
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="reason">标记原因</param>
    public void FlagPlayer(PlayerId playerId, string reason)
    {
        _flaggedPlayers.Add(playerId);
    }

    /// <summary>
    /// 移除蜜罐实体
    /// </summary>
    /// <param name="entityId">蜜罐实体 ID</param>
    public void RemoveTrapEntity(EntityId entityId)
    {
        _trapEntities.Remove(entityId);
    }

    /// <summary>
    /// 获取所有被标记的玩家 ID
    /// </summary>
    /// <returns>被标记的玩家 ID 集合</returns>
    public HashSet<PlayerId> GetFlaggedPlayers()
    {
        return [.._flaggedPlayers];
    }

    #endregion

    #region 内部记录

    private sealed record TrapEntity(EntityId EntityId, string TrapType, float X, float Y, float Z);

    private sealed record ProtocolTrapField(string FieldName, object FakeValue);

    #endregion
}
