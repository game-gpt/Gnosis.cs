using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using Gnosis.Scene.Stream;

namespace Gnosis.Scene.StoryCommands;

public sealed class SceneCommands
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
    /// Story 语法: %scene::change("bg_school", "fade")
    /// 参数: args[0] = 场景路径, args[1] = 过渡效果 (可选, 默认 "instant")
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.SceneChange, StoryCommandIds.SceneChange)]
    public object? StorySceneChange(IVMState vm, object?[] args)
    {
        var scenePath = args.ElementAtOrDefault(0)?.ToString();

        if (scenePath is null)
        {
            return null;
        }

        var transition = args.ElementAtOrDefault(1)?.ToString() ?? "instant";

        _streamer.LoadScene(scenePath);

        return null;
    }

    /// <summary>
    /// 场景过渡效果
    /// Story 语法: %scene::transition("fade", 1.0)
    /// 参数: args[0] = 过渡类型, args[1] = 过渡时长 (可选, 默认 1.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.SceneTransition, StoryCommandIds.SceneTransition)]
    public object? StorySceneTransition(IVMState vm, object?[] args)
    {
        var transitionType = args.ElementAtOrDefault(0)?.ToString() ?? "fade";
        var duration = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        return null;
    }

    #endregion
}
