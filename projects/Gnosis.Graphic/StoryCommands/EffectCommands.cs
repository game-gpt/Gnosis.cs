using Gnosis.Core.StoryCommands;
using Gnosis.Graphic.Pipeline;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Graphic.StoryCommands;

public sealed class EffectCommands
{
    #region 字段

    private readonly IRenderPipeline _pipeline;
    private readonly List<IRenderPass> _activeEffectPasses = new();

    #endregion

    #region 构造函数

    public EffectCommands(IRenderPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    #endregion

    #region Effect 命令

    /// <summary>
    /// 闪光特效
    /// Story 语法: %effect::flash(1.0, 1.0, 1.0, 0.5)
    /// 参数: args[0] = R (可选, 默认 1.0), args[1] = G (可选, 默认 1.0),
    ///       args[2] = B (可选, 默认 1.0), args[3] = 持续时间 (可选, 默认 0.5)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.EffectFlash, StoryCommandIds.EffectFlash)]
    public object? StoryEffectFlash(IVMState vm, object?[] args)
    {
        var r = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);
        var g = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);
        var b = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 1.0);
        var duration = Convert.ToSingle(args.ElementAtOrDefault(3) ?? 0.5);

        var flashPass = new RenderPass("StoryFlash", (context, cmd) =>
        {
        });

        _pipeline.AddPass(flashPass);
        _activeEffectPasses.Add(flashPass);

        return null;
    }

    /// <summary>
    /// 震动特效
    /// Story 语法: %effect::shake(0.5, 10.0)
    /// 参数: args[0] = 持续时间 (可选, 默认 0.5), args[1] = 强度 (可选, 默认 10.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.EffectShake, StoryCommandIds.EffectShake)]
    public object? StoryEffectShake(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 0.5);
        var intensity = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 10.0);

        var shakePass = new RenderPass("StoryShake", (context, cmd) =>
        {
        });

        _pipeline.AddPass(shakePass);
        _activeEffectPasses.Add(shakePass);

        return null;
    }

    /// <summary>
    /// 淡出特效
    /// Story 语法: %effect::fade_out(1.0, 0.0, 0.0, 0.0)
    /// 参数: args[0] = 持续时间 (可选, 默认 1.0),
    ///       args[1] = R (可选, 默认 0.0), args[2] = G (可选, 默认 0.0), args[3] = B (可选, 默认 0.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.EffectFadeOut, StoryCommandIds.EffectFadeOut)]
    public object? StoryEffectFadeOut(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);
        var r = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 0.0);
        var g = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 0.0);
        var b = Convert.ToSingle(args.ElementAtOrDefault(3) ?? 0.0);

        var fadePass = new RenderPass("StoryFadeOut", (context, cmd) =>
        {
        });

        _pipeline.AddPass(fadePass);
        _activeEffectPasses.Add(fadePass);

        return null;
    }

    /// <summary>
    /// 闪电特效
    /// Story 语法: %effect::lightning(0.3)
    /// 参数: args[0] = 持续时间 (可选, 默认 0.3)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.EffectLightning, StoryCommandIds.EffectLightning)]
    public object? StoryEffectLightning(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 0.3);

        var lightningPass = new RenderPass("StoryLightning", (context, cmd) =>
        {
        });

        _pipeline.AddPass(lightningPass);
        _activeEffectPasses.Add(lightningPass);

        return null;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 清理所有特效 Pass
    /// </summary>
    public void Cleanup()
    {
        foreach (var pass in _activeEffectPasses)
        {
            _pipeline.RemovePass(pass.Name);
        }

        _activeEffectPasses.Clear();
    }

    #endregion
}
