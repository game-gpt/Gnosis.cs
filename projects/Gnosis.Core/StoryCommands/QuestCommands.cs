namespace Gnosis.Core.StoryCommands;

public sealed class QuestCommands
{
    #region 事件

    public event Action<QuestState>? OnQuestStarted;
    public event Action<QuestState>? OnQuestCompleted;
    public event Action<QuestState>? OnQuestUpdated;

    #endregion

    #region 字段

    private readonly Dictionary<string, QuestState> _activeQuests = new();

    #endregion

    #region Quest 命令

    public object? StoryQuestStart(object?[] args)
    {
        var questId = args.ElementAtOrDefault(0)?.ToString();

        if (questId is null)
        {
            return null;
        }

        var description = args.ElementAtOrDefault(1)?.ToString() ?? "";

        var quest = new QuestState(questId, description, QuestStatus.Active, 0);
        _activeQuests[questId] = quest;

        OnQuestStarted?.Invoke(quest);

        return null;
    }

    public object? StoryQuestComplete(object?[] args)
    {
        var questId = args.ElementAtOrDefault(0)?.ToString();

        if (questId is null)
        {
            return null;
        }

        if (_activeQuests.TryGetValue(questId, out var quest))
        {
            quest.Status = QuestStatus.Completed;
            OnQuestCompleted?.Invoke(quest);
        }

        return null;
    }

    public object? StoryQuestUpdate(object?[] args)
    {
        var questId = args.ElementAtOrDefault(0)?.ToString();

        if (questId is null)
        {
            return null;
        }

        var progress = Convert.ToInt32(args.ElementAtOrDefault(1) ?? 1);

        if (_activeQuests.TryGetValue(questId, out var quest))
        {
            quest.Progress += progress;
            OnQuestUpdated?.Invoke(quest);
        }

        return null;
    }

    #endregion

    #region 公开方法

    public QuestState? GetQuest(string questId)
    {
        return _activeQuests.TryGetValue(questId, out var quest) ? quest : null;
    }

    public IReadOnlyList<QuestState> GetActiveQuests()
    {
        return _activeQuests.Values.Where(q => q.Status == QuestStatus.Active).ToList();
    }

    #endregion
}

public sealed class QuestState
{
    #region 属性

    public string Id { get; }
    public string Description { get; set; }
    public QuestStatus Status { get; set; }
    public int Progress { get; set; }

    #endregion

    #region 构造函数

    public QuestState(string id, string description, QuestStatus status, int progress)
    {
        Id = id;
        Description = description;
        Status = status;
        Progress = progress;
    }

    #endregion
}

public enum QuestStatus
{
    Inactive,
    Active,
    Completed,
    Failed
}
