namespace Gnosis.Core.StoryCommands;

public sealed class QuestCommands : IStoryQuestCommands
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

    public void StartQuest(string questId, string description)
    {
        if (questId is null)
        {
            return;
        }

        var quest = new QuestState(questId, description, QuestStatus.Active, 0);
        _activeQuests[questId] = quest;

        OnQuestStarted?.Invoke(quest);
    }

    public void CompleteQuest(string questId)
    {
        if (questId is null)
        {
            return;
        }

        if (_activeQuests.TryGetValue(questId, out var quest))
        {
            quest.Status = QuestStatus.Completed;
            OnQuestCompleted?.Invoke(quest);
        }
    }

    public void UpdateQuest(string questId, int progress)
    {
        if (questId is null)
        {
            return;
        }

        if (_activeQuests.TryGetValue(questId, out var quest))
        {
            quest.Progress += progress;
            OnQuestUpdated?.Invoke(quest);
        }
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
