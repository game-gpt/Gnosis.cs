using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using Gnosis.Scene.Stream;

namespace Gnosis.Scene.StoryCommands;

public sealed class SceneCommands : IStorySceneCommands
{
    #region 字段

    private readonly SceneStreamer _streamer;

    #endregion

    #region 构造函数

    public SceneCommands(SceneStreamer streamer)
    {
        _streamer = streamer;
    }

    #endregion

    #region 场景命令

    /// <summary>
    /// 切换场景背景
    /// </summary>
    public void ChangeScene(string scenePath, string transition)
    {
        if (scenePath is null)
        {
            return;
        }

        _streamer.LoadScene(scenePath);
    }

    /// <summary>
    /// 场景过渡效果
    /// </summary>
    public void SceneTransition(string type, float duration)
    {
        var transitionType = type ?? "fade";
    }

    #endregion

    #region VM 绑定方法

    [NativeFunctionBinding(StoryCommandNames.SceneChange, StoryCommandIds.SceneChange)]
    public object? StorySceneChange(IVMState vm, object?[] args)
    {
        var scenePath = args.ElementAtOrDefault(0)?.ToString();
        var transition = args.ElementAtOrDefault(1)?.ToString() ?? "instant";

        ChangeScene(scenePath ?? "", transition);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.SceneTransition, StoryCommandIds.SceneTransition)]
    public object? StorySceneTransition(IVMState vm, object?[] args)
    {
        var transitionType = args.ElementAtOrDefault(0)?.ToString() ?? "fade";
        var duration = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        SceneTransition(transitionType, duration);
        return null;
    }

    #endregion
}
