namespace Gnosis.Network.Lobby;

public sealed class SkillBasedMatchmaker : IMatchmaker
{
    #region 字段

    private readonly List<MatchmakingEntry> _queue = new();
    private MatchCriteria? _currentCriteria;
    private DateTime _matchStartTime;
    private readonly LobbyService _lobbyService;

    #endregion

    #region 属性

    public bool IsMatching => _currentCriteria is not null;

    public int QueueSize => _queue.Count;

    #endregion

    #region 事件

    public event Action<LobbyRoom>? OnMatchFound;
    public event Action? OnMatchTimeout;

    #endregion

    #region 构造函数

    public SkillBasedMatchmaker(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    #endregion

    #region IMatchmaker 实现

    public void StartMatching(MatchCriteria criteria)
    {
        _currentCriteria = criteria;
        _matchStartTime = DateTime.UtcNow;
    }

    public void CancelMatching()
    {
        _currentCriteria = null;
        _queue.Clear();
    }

    #endregion

    #region 公开方法

    public void AddToQueue(string playerId, string playerName, int skillLevel, Dictionary<string, string>? customData = null)
    {
        var entry = new MatchmakingEntry
        {
            PlayerId = playerId,
            PlayerName = playerName,
            SkillLevel = skillLevel,
            EnqueueTime = DateTime.UtcNow,
            CustomData = customData ?? new Dictionary<string, string>()
        };

        _queue.Add(entry);
    }

    public void RemoveFromQueue(string playerId)
    {
        _queue.RemoveAll(e => e.PlayerId == playerId);
    }

    public void Update(float delta)
    {
        if (_currentCriteria == null)
        {
            return;
        }

        var elapsed = (DateTime.UtcNow - _matchStartTime).TotalSeconds;

        if (elapsed > _currentCriteria.TimeoutSeconds)
        {
            _currentCriteria = null;
            OnMatchTimeout?.Invoke();
            return;
        }

        var matchResult = FindMatch(_currentCriteria);

        if (matchResult != null)
        {
            _currentCriteria = null;
            OnMatchFound?.Invoke(matchResult);
        }
    }

    #endregion

    #region 私有方法 - 匹配算法

    private LobbyRoom? FindMatch(MatchCriteria criteria)
    {
        var eligibleEntries = _queue.Where(e =>
            e.SkillLevel >= criteria.MinSkillLevel &&
            e.SkillLevel <= criteria.MaxSkillLevel).ToList();

        if (eligibleEntries.Count < criteria.RequiredPlayers)
        {
            var expandedEntries = ExpandSearchRange(eligibleEntries, criteria);
            if (expandedEntries.Count < criteria.RequiredPlayers)
            {
                return null;
            }

            eligibleEntries = expandedEntries;
        }

        var matchedPlayers = SelectBestMatch(eligibleEntries, criteria.RequiredPlayers);

        var roomOptions = new LobbyRoomOptions
        {
            Name = $"匹配房间 {DateTime.UtcNow:HHmmss}",
            MaxPlayers = criteria.RequiredPlayers * 2,
            Properties = new Dictionary<string, string>
            {
                ["game_mode"] = criteria.GameMode,
                ["match_type"] = "skill_based"
            }
        };

        if (criteria.CustomParams != null)
        {
            foreach (var (key, value) in criteria.CustomParams)
            {
                roomOptions.Properties[key] = value;
            }
        }

        var room = _lobbyService.CreateRoom(roomOptions);

        foreach (var player in matchedPlayers)
        {
            _queue.Remove(player);
        }

        return room;
    }

    private List<MatchmakingEntry> ExpandSearchRange(List<MatchmakingEntry> current, MatchCriteria criteria)
    {
        var waitTime = (DateTime.UtcNow - _matchStartTime).TotalSeconds;
        var expansionFactor = Math.Min(waitTime / criteria.TimeoutSeconds, 1.0);

        var skillExpansion = (int)(expansionFactor * 50);

        var minSkill = Math.Max(0, criteria.MinSkillLevel - skillExpansion);
        var maxSkill = Math.Min(100, criteria.MaxSkillLevel + skillExpansion);

        return _queue.Where(e =>
            e.SkillLevel >= minSkill &&
            e.SkillLevel <= maxSkill).ToList();
    }

    private List<MatchmakingEntry> SelectBestMatch(List<MatchmakingEntry> entries, int requiredCount)
    {
        if (entries.Count <= requiredCount)
        {
            return entries.OrderBy(e => e.EnqueueTime).Take(requiredCount).ToList();
        }

        var sorted = entries.OrderBy(e => e.SkillLevel).ToList();

        var bestGroup = new List<MatchmakingEntry>();
        var bestVariance = float.MaxValue;

        for (int i = 0; i <= sorted.Count - requiredCount; i++)
        {
            var group = sorted.Skip(i).Take(requiredCount).ToList();
            var variance = CalculateSkillVariance(group);

            if (variance < bestVariance)
            {
                bestVariance = variance;
                bestGroup = group;
            }
        }

        return bestGroup;
    }

    private float CalculateSkillVariance(List<MatchmakingEntry> group)
    {
        if (group.Count <= 1)
        {
            return 0f;
        }

        var avg = group.Average(e => e.SkillLevel);
        var variance = group.Sum(e => Math.Pow(e.SkillLevel - avg, 2)) / group.Count;

        return (float)variance;
    }

    #endregion

    #region 内部类型

    private sealed class MatchmakingEntry
    {
        public string PlayerId { get; init; } = "";
        public string PlayerName { get; init; } = "";
        public int SkillLevel { get; init; }
        public DateTime EnqueueTime { get; init; }
        public Dictionary<string, string> CustomData { get; init; } = new();
    }

    #endregion
}
