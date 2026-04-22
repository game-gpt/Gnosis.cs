namespace Gnosis.Core.StoryCommands;

public sealed class StoryCommandRegistry
{
    #region 字段

    private readonly List<StoryCommandDescriptor> _commands = new();

    #endregion

    #region 属性

    public IReadOnlyList<StoryCommandDescriptor> Commands => _commands;

    #endregion

    #region 构造函数

    public StoryCommandRegistry()
    {
        RegisterAudioCommands();
        RegisterSceneCommands();
        RegisterCharacterCommands();
        RegisterEffectCommands();
        RegisterQuestCommands();
        RegisterDialogueCommands();
    }

    #endregion

    #region 私有方法

    private void RegisterAudioCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.AudioPlay, StoryCommandNames.AudioPlay, "audio", "播放背景音乐"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.AudioStop, StoryCommandNames.AudioStop, "audio", "停止背景音乐"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.AudioPlaySe, StoryCommandNames.AudioPlaySe, "audio", "播放音效"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.AudioFadeOut, StoryCommandNames.AudioFadeOut, "audio", "淡出背景音乐"));
    }

    private void RegisterSceneCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.SceneChange, StoryCommandNames.SceneChange, "scene", "切换场景背景"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.SceneTransition, StoryCommandNames.SceneTransition, "scene", "场景过渡效果"));
    }

    private void RegisterCharacterCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.CharacterShow, StoryCommandNames.CharacterShow, "character", "显示角色立绘"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.CharacterHide, StoryCommandNames.CharacterHide, "character", "隐藏角色立绘"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.CharacterMove, StoryCommandNames.CharacterMove, "character", "移动角色立绘"));
    }

    private void RegisterEffectCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.EffectFlash, StoryCommandNames.EffectFlash, "effect", "闪光特效"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.EffectShake, StoryCommandNames.EffectShake, "effect", "震动特效"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.EffectFadeOut, StoryCommandNames.EffectFadeOut, "effect", "淡出特效"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.EffectLightning, StoryCommandNames.EffectLightning, "effect", "闪电特效"));
    }

    private void RegisterQuestCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.QuestStart, StoryCommandNames.QuestStart, "quest", "开始任务"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.QuestComplete, StoryCommandNames.QuestComplete, "quest", "完成任务"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.QuestUpdate, StoryCommandNames.QuestUpdate, "quest", "更新任务"));
    }

    private void RegisterDialogueCommands()
    {
        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.DialogueShow, StoryCommandNames.DialogueShow, "dialogue", "显示对话框"));

        _commands.Add(new StoryCommandDescriptor(
            StoryCommandIds.DialogueHide, StoryCommandNames.DialogueHide, "dialogue", "隐藏对话框"));
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 按模块获取命令列表
    /// </summary>
    public IReadOnlyList<StoryCommandDescriptor> GetCommandsByModule(string module)
    {
        return _commands.Where(c => c.Module == module).ToList();
    }

    /// <summary>
    /// 按名称查找命令
    /// </summary>
    public StoryCommandDescriptor? GetCommandByName(string name)
    {
        return _commands.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// 按 ID 查找命令
    /// </summary>
    public StoryCommandDescriptor? GetCommandById(int id)
    {
        return _commands.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// 获取所有模块名称
    /// </summary>
    public IReadOnlyList<string> GetAllModules()
    {
        return _commands.Select(c => c.Module).Distinct().OrderBy(m => m).ToList();
    }

    /// <summary>
    /// 将所有命令注册到原生函数注册表
    /// </summary>
    public void RegisterAll(Action<int, string, Type, object?> registerAction)
    {
        foreach (var command in _commands)
        {
            registerAction(command.Id, command.Name, typeof(void), null);
        }
    }

    #endregion
}

public sealed class StoryCommandDescriptor
{
    #region 属性

    public int Id { get; }
    public string Name { get; }
    public string Module { get; }
    public string Description { get; }

    #endregion

    #region 构造函数

    public StoryCommandDescriptor(int id, string name, string module, string description)
    {
        Id = id;
        Name = name;
        Module = module;
        Description = description;
    }

    #endregion
}
