using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Verse;

/// <summary>
/// Verse 剧本运行时，管理场景切换、标签跳转、变量状态
/// </summary>
public sealed class VerseStoryRuntime
{
    #region 事件

    /// <summary>
    /// 场景切换事件
    /// </summary>
    public event Action<string>? OnSceneChanged;

    /// <summary>
    /// 标签到达事件
    /// </summary>
    public event Action<string>? OnLabelReached;

    /// <summary>
    /// 对话显示事件
    /// </summary>
    public event Action<string?, string, string?>? OnDialogueShown;

    /// <summary>
    /// 叙述显示事件
    /// </summary>
    public event Action<string>? OnNarrationShown;

    /// <summary>
    /// 菜单选择事件
    /// </summary>
    public event Action<string, IReadOnlyList<string>>? OnMenuPresented;

    /// <summary>
    /// 暂停事件
    /// </summary>
    public event Action<double>? OnPaused;

    /// <summary>
    /// 等待事件
    /// </summary>
    public event Action<double>? OnWaited;

    /// <summary>
    /// 变量变更事件
    /// </summary>
    public event Action<string, object?>? OnVariableChanged;

    #endregion

    #region 字段

    private readonly Dictionary<string, object?> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VerseSceneInfo> _scenes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _labels = new(StringComparer.Ordinal);
    private string _currentScene = string.Empty;
    private string _currentLabel = string.Empty;

    #endregion

    #region 属性

    /// <summary>
    /// 当前场景名称
    /// </summary>
    public string CurrentScene => _currentScene;

    /// <summary>
    /// 当前标签
    /// </summary>
    public string CurrentLabel => _currentLabel;

    /// <summary>
    /// 所有变量
    /// </summary>
    public IReadOnlyDictionary<string, object?> Variables => _variables;

    #endregion

    #region 场景管理

    /// <summary>
    /// 注册场景
    /// </summary>
    public void RegisterScene(string name, VerseSceneInfo scene)
    {
        _scenes[name] = scene;
    }

    /// <summary>
    /// 切换到指定场景
    /// </summary>
    public bool SwitchScene(string sceneName)
    {
        if (!_scenes.ContainsKey(sceneName))
        {
            return false;
        }

        _currentScene = sceneName;
        _currentLabel = string.Empty;
        OnSceneChanged?.Invoke(sceneName);
        return true;
    }

    /// <summary>
    /// 获取场景信息
    /// </summary>
    public VerseSceneInfo? GetScene(string name)
    {
        return _scenes.TryGetValue(name, out var scene) ? scene : null;
    }

    #endregion

    #region 标签管理

    /// <summary>
    /// 注册标签
    /// </summary>
    public void RegisterLabel(string labelName, string sceneName)
    {
        _labels[labelName] = sceneName;
    }

    /// <summary>
    /// 跳转到标签
    /// </summary>
    public bool JumpToLabel(string labelName)
    {
        if (!_labels.TryGetValue(labelName, out var sceneName))
        {
            return false;
        }

        _currentScene = sceneName;
        _currentLabel = labelName;
        OnLabelReached?.Invoke(labelName);
        return true;
    }

    #endregion

    #region 变量管理

    /// <summary>
    /// 设置变量
    /// </summary>
    public void SetVariable(string name, object? value)
    {
        _variables[name] = value;
        OnVariableChanged?.Invoke(name, value);
    }

    /// <summary>
    /// 获取变量
    /// </summary>
    public object? GetVariable(string name)
    {
        return _variables.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// 检查变量是否存在
    /// </summary>
    public bool HasVariable(string name)
    {
        return _variables.ContainsKey(name);
    }

    #endregion

    #region Story 命令执行

    /// <summary>
    /// 显示对话
    /// </summary>
    public void ShowDialogue(string? speaker, string text, string? emotion)
    {
        OnDialogueShown?.Invoke(speaker, text, emotion);
    }

    /// <summary>
    /// 显示叙述
    /// </summary>
    public void ShowNarration(string text)
    {
        OnNarrationShown?.Invoke(text);
    }

    /// <summary>
    /// 显示菜单
    /// </summary>
    public int PresentMenu(string title, IReadOnlyList<string> choices)
    {
        OnMenuPresented?.Invoke(title, choices);
        return 0;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause(double duration)
    {
        OnPaused?.Invoke(duration);
    }

    /// <summary>
    /// 等待
    /// </summary>
    public void Wait(double duration)
    {
        OnWaited?.Invoke(duration);
    }

    #endregion

    #region 重置

    /// <summary>
    /// 重置运行时状态
    /// </summary>
    public void Reset()
    {
        _variables.Clear();
        _currentScene = string.Empty;
        _currentLabel = string.Empty;
    }

    #endregion
}

/// <summary>
/// Verse 场景信息
/// </summary>
public sealed class VerseSceneInfo
{
    public string Name { get; }
    public IReadOnlyList<string> Labels { get; }

    public VerseSceneInfo(string name, IReadOnlyList<string> labels)
    {
        Name = name;
        Labels = labels;
    }
}
